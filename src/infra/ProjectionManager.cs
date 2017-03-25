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
		private readonly string _clusterDns;
		private readonly int _externalHttpPort;
		private readonly ILogger _logger;


		public ProjectionManager(string clusterDns, int externalHttpPort, ILogger logger)
		{
			_clusterDns = clusterDns;
			_externalHttpPort = externalHttpPort;
			_logger = logger;
		}

		public  Task CreateOrUpdateProjection(string projectionName, string projectionDefinition, UserCredentials userCredentials, int maxAttempts)
		{
			return Execute(_clusterDns, _externalHttpPort, _logger, maxAttempts, async (manager, attempt) =>
				{
					try
					{
						await manager.CreateContinuousAsync(projectionName, projectionDefinition, userCredentials);
					}
					catch (ProjectionCommandFailedException ex) when (ex.HttpStatusCode == (int)HttpStatusCode.Conflict)
					{
						var storedProjectionDefinition = await manager.GetQueryAsync(projectionName, userCredentials);
						if (!string.Equals(storedProjectionDefinition, projectionDefinition))
						{
							await manager.UpdateQueryAsync(projectionName, projectionDefinition, userCredentials);
						}
					}
					catch(Exception ex)
					{
						_logger.Info("Creating or updating projection {0} attempt {1}/{2} failed. {3}", projectionName, attempt, maxAttempts, ex.Message);
						throw;
					}
				});
		}

		private static async Task Execute(string clusterDns, int externalHttpPort, ILogger logger, int maxAttempts, Func<ProjectionsManager, int, Task> operation)
		{
			var httpEndpoints = Dns.GetHostEntry(clusterDns)
				.AddressList
				.Select(x => new IPEndPoint(x, externalHttpPort))
				.ToArray();
			for (var attempt = 1; maxAttempts >= attempt; ++attempt)
			{
				for (int i = 0, j = httpEndpoints.Length; j > i; ++i)
				{
					var manager = new ProjectionsManager(logger, httpEndpoints[i], TimeSpan.FromMilliseconds(5000));
					try
					{
						await operation(manager, attempt);
						return;
					}
					catch when (maxAttempts > attempt)
					{
						await Task.Delay(500);
					}
				}
			}
		}
	}
}
