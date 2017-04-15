using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using EventStore.ClientAPI;
using EventStore.ClientAPI.Exceptions;
using EventStore.ClientAPI.Projections;
using EventStore.ClientAPI.SystemData;

using shared;

namespace eventstore
{
    public interface IProjectionManager
    {
        Task CreateOrUpdateContinuousProjection(string name, string query, int maxAttempts = int.MaxValue);
    }

    public class ProjectionManager : IProjectionManager
    {
        private readonly string _clusterDns;
        private readonly int _externalHttpPort;
        private readonly string _username;
        private readonly string _password;
        private readonly ILogger _logger;

        public ProjectionManager(string clusterDns, int externalHttpPort, string username, string password, ILogger logger)
        {
            _clusterDns = clusterDns;
            _externalHttpPort = externalHttpPort;
            _username = username;
            _password = password;
            _logger = logger;
        }

        public Task CreateOrUpdateContinuousProjection(string name, string query, int maxAttempts)
        {
            return Retry.RetryUntil(
                x =>
                    Execute(
                        _clusterDns,
                        _externalHttpPort,
                        _logger,
                        async manager =>
                        {
                            var userCredentials = new UserCredentials(_username, _password);
                            try
                            {
                                await manager.CreateContinuousAsync(name, query, true, userCredentials);
                                _logger.Info("Projection {0} created.", name);
                            }
                            catch (ProjectionCommandConflictException)
                            {
                                var storedQuery = await manager.GetQueryAsync(name, userCredentials);
                                if (!string.Equals(query, storedQuery))
                                {
                                    await manager.UpdateQueryAsync(name, query, userCredentials);
                                    _logger.Info("Projection {0} updated.", name);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.Error(ex, "Failed to create projection {0}.", name);
                                return false;
                            }
                            return true;
                        }),
                result => result,
                maxAttempts);
        }

        private static Task<bool> Execute(string clusterDns, int externalHttpPort, ILogger logger, Func<ProjectionsManager, Task<bool>> operation)
        {
            var httpEndpoints = Dns.GetHostEntry(clusterDns)
                .AddressList
                .Select(ipAddress =>
                    new IPEndPoint(ipAddress, externalHttpPort))
                .ToArray();
            return httpEndpoints
                .AnyAsync(httpEndpoint =>
                    operation(new ProjectionsManager(logger, httpEndpoint, TimeSpan.FromSeconds(5))));
        }

    }
}
