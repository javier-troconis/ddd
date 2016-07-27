using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using EventStore.ClientAPI;
using EventStore.ClientAPI.Exceptions;
using EventStore.ClientAPI.Projections;
using EventStore.ClientAPI.SystemData;

namespace infra
{
    public class ProjectionManager
    {
        private readonly ILogger _logger;
        private readonly IPEndPoint[] _httpEndPoints;

        private ProjectionManager(ILogger logger)
        {
            _logger = logger;
        }

        public ProjectionManager(ILogger logger, IPEndPoint httpEndPoint) 
            : this(logger)
        {
            _httpEndPoints = new [] { httpEndPoint };
        }

        public ProjectionManager(ILogger logger, string clusterDns, int externalHttpPort)
             : this(logger)
        {
            _httpEndPoints = Dns.GetHostEntry(clusterDns)
                .AddressList
                .Select(x => new IPEndPoint(x, externalHttpPort))
                .ToArray();
        }

        public async Task CreateOrUpdateProjectionAsync(string projectionName, string projectionDefinition, UserCredentials userCredentials,  int maxAttempts)
        {
            for (var attempt = 1; maxAttempts >= attempt; ++attempt)
            {
                for(int i = 0, j = _httpEndPoints.Length; j > i; ++i)
                {
                    var projectionManager = new ProjectionsManager(_logger, _httpEndPoints[i], TimeSpan.FromMilliseconds(5000));
                    try
                    {
                        var isProjectionNew = await TryCreateProjection(projectionManager, userCredentials, projectionName, projectionDefinition);
                        if (!isProjectionNew && await HasProjectionChanged(projectionManager, userCredentials, projectionName, projectionDefinition))
                        {
                            await UpdateProjection(projectionManager, userCredentials, projectionName, projectionDefinition);
                        }
                        return;
                    }
                    catch(Exception)
                    {
                        _logger.Info("Creating or updating projection {0} attempt {1}/{2} failed: {3}", projectionName, attempt, maxAttempts, _httpEndPoints[i].Address);
                    }
                }
                await Task.Delay(500);
            }
            throw new Exception($"Failed to create or update projection {projectionName} in {maxAttempts}");
        }

        private static async Task<bool> TryCreateProjection(ProjectionsManager projectionManager, UserCredentials userCredentials, string projectionName, string projectionDefinition)
        {
            try
            {
                await projectionManager.CreateContinuousAsync(projectionName, projectionDefinition, userCredentials);
                return true;
            }
            catch (ProjectionCommandFailedException ex)
            {
                if (ex.HttpStatusCode != (int)HttpStatusCode.Conflict)
                {
                    throw;
                }
                return false;
            }
        }

        private static async Task<bool> HasProjectionChanged(ProjectionsManager projectionManager, UserCredentials userCredentials, string projectionName, string projectionDefinition)
        {
            var storedProjectionDefinition = await projectionManager.GetQueryAsync(projectionName, userCredentials);
            return storedProjectionDefinition != projectionDefinition;
        }

        private static Task UpdateProjection(ProjectionsManager projectionManager, UserCredentials userCredentials, string projectionName, string projectionDefinition)
        {
            return projectionManager.UpdateQueryAsync(projectionName, projectionDefinition, userCredentials);
        }
    }
}
