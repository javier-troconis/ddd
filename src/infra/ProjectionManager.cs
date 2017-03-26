using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using shared;
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

		public Task CreateContinuousProjection(string name, string query, int maxAttempts)
		{
			return Execute(_clusterDns, _externalHttpPort, _logger, maxAttempts, async (manager, attempt) =>
			{
				try
				{
					await manager.CreateContinuousAsync(name, query, true, new UserCredentials(_username, _password));
				}
				catch (ProjectionCommandConflictException)
				{
					throw;
				}
				catch (Exception ex)
				{
					_logger.Info("Creating projection {0} attempt {1}/{2} failed. {3}", name, attempt, maxAttempts, ex.Message);
					return false;
				}
				_logger.Info("Projection {0} created.", name);
				return true;
			});
		}

		public Task UpdateProjection(string name, string newQuery, int maxAttempts)
		{
			return Execute(_clusterDns, _externalHttpPort, _logger, maxAttempts, async (manager, attempt) =>
			{
				try
				{
					await manager.UpdateQueryAsync(name, newQuery, new UserCredentials(_username, _password));
				}
				catch (ProjectionCommandConflictException)
				{
					throw;
				}
				catch (Exception ex)
				{
					_logger.Info("Updating projection {0} attempt {1}/{2} failed. {3}", name, attempt, maxAttempts, ex.Message);
					return false;
				}
				_logger.Info("Projection {0} updated.", name);
				return true;
			});
		}

		private static async Task Execute(string clusterDns, int externalHttpPort, ILogger logger, int maxAttempts, Func<ProjectionsManager, int, Task<bool>> operation)
		{
			var httpEndpoints = Dns.GetHostEntry(clusterDns)
				.AddressList
				.Select(ipAddress => 
					new IPEndPoint(ipAddress, externalHttpPort));
			var managers = httpEndpoints
				.Select(httpEndpoint =>
					new ProjectionsManager(logger, httpEndpoint, TimeSpan.FromMilliseconds(5000)))
				.ToArray();
			for (var attempt = 1; maxAttempts >= attempt; ++attempt)
			{
				var succeeded = await managers.AnyAsync(x => operation(x, attempt));
				if (succeeded)
				{
					break;
				}
				if (maxAttempts > attempt)
				{
					await Task.Delay(500);
				}
			}
		}
	}
}
