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

		public Task CreateProjection(string projectionName, string projectionDefinition, UserCredentials userCredentials, int maxAttempts)
		{
			return Execute(_clusterDns, _externalHttpPort, _logger, maxAttempts, async (manager, attempt) =>
			{
				try
				{
					await manager.CreateContinuousAsync(projectionName, projectionDefinition, userCredentials);
				}
				catch (Exception ex)
				{
					_logger.Info("Creating projection {0} attempt {1}/{2} failed. {3}", projectionName, attempt, maxAttempts, ex.Message);
					throw;
				}
				_logger.Info("Projection {0} created.", projectionName);
			});
		}

		public Task UpdateProjection(string projectionName, string projectionDefinition, UserCredentials userCredentials, int maxAttempts)
		{
			return Execute(_clusterDns, _externalHttpPort, _logger, maxAttempts, async (manager, attempt) =>
			{
				try
				{
					var storedProjectionDefinition = await manager.GetQueryAsync(projectionName, userCredentials);
					if (string.Equals(storedProjectionDefinition, projectionDefinition))
					{
						return;
					}
					await manager.UpdateQueryAsync(projectionName, projectionDefinition, userCredentials);
				}
				catch (Exception ex)
				{
					_logger.Info("Updating projection {0} attempt {1}/{2} failed. {3}", projectionName, attempt, maxAttempts, ex.Message);
					throw;
				}
				_logger.Info("Projection {0} updated.", projectionName);
			});
		}

		private static async Task Execute(string clusterDns, int externalHttpPort, ILogger logger, int maxAttempts, Func<ProjectionsManager, int, Task> operation)
		{
			var httpEndpoints = Dns.GetHostEntry(clusterDns)
				.AddressList
				.Select(x => 
					new IPEndPoint(x, externalHttpPort));
			var managers = httpEndpoints
				.Select(httpEndpoint => 
					new ProjectionsManager(logger, httpEndpoint, TimeSpan.FromMilliseconds(5000)))
				.ToArray();
			for (var attempt = 1; maxAttempts >= attempt; ++attempt)
			{
				foreach (var manager in managers)
				{
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
