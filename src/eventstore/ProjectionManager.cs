using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

using EventStore.ClientAPI;
using EventStore.ClientAPI.Exceptions;
using EventStore.ClientAPI.Projections;
using EventStore.ClientAPI.SystemData;
using Newtonsoft.Json;
using shared;

namespace eventstore
{
    public interface IProjectionStatistics
    {
        double Progress { get; }
    }

    public interface IProjectionManager
    {
        Task CreateOrUpdateContinuousProjection(string name, string query, UserCredentials credentials = null);
        Task ResetProjection(string name, UserCredentials credentials = null);
        Task DisableProjection(string name, UserCredentials credentials = null);
        Task EnableProjection(string name, UserCredentials credentials = null);
        Task<IProjectionStatistics> GetProjectionStatistics(string name, UserCredentials credentials = null);
        Task<bool> CompareProjectionQuery(string name, string query, UserCredentials credentials = null);
    }

    public sealed class ProjectionManager : IProjectionManager
    {
        private readonly string _clusterDns;
        private readonly int _externalHttpPort;
        private readonly ILogger _logger;

        public ProjectionManager(string clusterDns, int externalHttpPort, ILogger logger)
        {
            _clusterDns = clusterDns;
            _externalHttpPort = externalHttpPort;
            _logger = logger;
        }

        public Task CreateOrUpdateContinuousProjection(string name, string query, UserCredentials credentials = null)
        {
            return Execute
                (
                    async x =>
                    {
                        var manager = new ProjectionsManager(_logger, x, TimeSpan.FromSeconds(5));
                        try
                        {
                            await manager.CreateContinuousAsync(name, query, true, credentials);
                            _logger.Info("Projection created");
                        }
                        catch (ProjectionCommandConflictException)
                        {
                            var storedQuery = await manager.GetQueryAsync(name, credentials);
                            if (!string.Equals(query, storedQuery))
                            {
                                await manager.UpdateQueryAsync(name, query, credentials);
                                _logger.Info("Projection updated");
                            }
                        }
                    },
                    x => _logger.Error(x, "Failed to create or update projection")
                );
        }

        public Task ResetProjection(string name, UserCredentials credentials = null)
        {
            return Execute
            (
                async x =>
                {
                    var uriString = $"http://{x.Address.ToString()}:{x.Port}/projection/{name}/command/reset";
                    var request = new HttpRequestMessage
                    {
                        RequestUri = new Uri(uriString),
                        Method = HttpMethod.Post
                    };
                    if (credentials != null)
                    {
                        var httpAuthentication = string.Format("{0}:{1}", credentials.Username, credentials.Password);
                        var encodedCredentials = Convert.ToBase64String(new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(httpAuthentication));
                        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", encodedCredentials);
                    }
                    var httpClient = new HttpClient();
                    await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                    _logger.Info("Projection reset");
                },
                x => _logger.Error(x, "Failed to reset projection")
            );
        }

        public Task DisableProjection(string name, UserCredentials credentials = null)
        {
            return Execute
            (
                async x =>
                {
                    var manager = new ProjectionsManager(_logger, x, TimeSpan.FromSeconds(5));
                    await manager.DisableAsync(name, credentials);
                    _logger.Info("Projection disabled");
                },
                x => _logger.Error(x, "Failed to disable projection")
            );
        }

        public Task EnableProjection(string name, UserCredentials credentials = null)
        {
            return Execute
            (
                async x =>
                {
                    var manager = new ProjectionsManager(_logger, x, TimeSpan.FromSeconds(5));
                    await manager.EnableAsync(name, credentials);
                    _logger.Info("Projection enabled");
                },
                x => _logger.Error(x, "Failed to enable projection")
            );
        }

        public async Task<IProjectionStatistics> GetProjectionStatistics(string name, UserCredentials credentials = null)
        {
            return await Execute
                (
                    async x =>
                    {
                        var manager = new ProjectionsManager(_logger, x, TimeSpan.FromSeconds(5));
                        var getStatisticsResult = await manager.GetStatisticsAsync(name, credentials);
                        var getProjectionStatisticsResponse = JsonConvert.DeserializeObject<GetProjectionStatisticsResponse>(getStatisticsResult);
                        return getProjectionStatisticsResponse.Projections[0];
                    },
                    x => _logger.Error(x, "Failed to get projection statistics")
                );
        }

        public async Task<bool> CompareProjectionQuery(string name, string query, UserCredentials credentials = null)
        {
            return await Execute
            (
                async x =>
                {
                    var manager = new ProjectionsManager(_logger, x, TimeSpan.FromSeconds(5));
                    var storedQuery = await manager.GetQueryAsync(name, credentials);
                    return string.Equals(query, storedQuery);
                },
                x => _logger.Error(x, "Failed to compare projection query")
            );
        }

        private Task Execute(Func<IPEndPoint, Task> action, Action<Exception> failed)
        {
            return Execute
            (
                async x =>
                {
                    await action(x);
                    return true;
                },
                failed
            );
        }

        private async Task<TResult> Execute<TResult>(Func<IPEndPoint, Task<TResult>> action, Action<Exception> failed)
        {
            var result = default(TResult);
            var endPoints = Dns.GetHostEntry(_clusterDns)
                .AddressList
                .Select(ipAddress => new IPEndPoint(ipAddress, _externalHttpPort))
                .ToArray();
            await endPoints
                .AnyAsync(async (endPoint, index) =>
                {
                    try
                    {
                        result = await action(endPoint);
                        return true;
                    }
                    catch when (endPoints.Length > index + 1)
                    {
                        return false;
                    }
                    catch (Exception ex)
                    {
                        failed(ex);
                        throw;
                    }
                });
            return result;
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        private class ProjectionStatistics : IProjectionStatistics
        {
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public double Progress { get; set; }
        }


        // ReSharper disable once ClassNeverInstantiated.Local
        private class GetProjectionStatisticsResponse
        {
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public ProjectionStatistics[] Projections { get; set; }
        }
    }
}
