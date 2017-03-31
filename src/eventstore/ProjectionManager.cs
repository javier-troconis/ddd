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
		Task CreateOrUpdateContinuousProjection(string name, string query, int maxAttempts = 1);
	}

	public class ProjectionManager : IProjectionManager
	{
		private readonly string _clusterDns;
		private readonly int _externalHttpPort;
		private readonly string _username;
		private readonly string _password;
		private readonly ILogger _logger;

        // do we need credentials ???
		public ProjectionManager(string clusterDns, int externalHttpPort, string username, string password, ILogger logger)
		{
			_clusterDns = clusterDns;
			_externalHttpPort = externalHttpPort;
			_username = username;
			_password = password;
			_logger = logger;
		}

		public async Task CreateOrUpdateContinuousProjection(string name, string query, int maxAttempts)
		{
			try
			{
				await CreateContinuousProjection(name, query, maxAttempts);
			}
			catch (ProjectionCommandConflictException)
			{
				await UpdateProjection(name, query, maxAttempts);
			}
		}

		private async Task<string> GetProjectionQuery(string name,int maxAttempts)
		{
			string query = string.Empty;
			await Execute(_clusterDns, _externalHttpPort, _logger, maxAttempts, async (manager, attempt) =>
			{
				try
				{
					query = await manager.GetQueryAsync(name, new UserCredentials(_username, _password));
				}
				catch (ProjectionCommandFailedException ex) when (ex.HttpStatusCode == (int)HttpStatusCode.NotFound)
				{
					throw;
				}
				catch (Exception ex)
				{
					_logger.Info("Fetching projection {0} query attempt {1}/{2} failed. {3}", name, attempt, maxAttempts, ex.Message);
					return false;
				}
				_logger.Info("Projection query {0} fetched.", name);
				return true;
			});
			return query;
		}

		private Task CreateContinuousProjection(string name, string query, int maxAttempts)
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

		private Task UpdateProjection(string name, string newQuery, int maxAttempts)
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
					new ProjectionsManager(logger, httpEndpoint, TimeSpan.FromSeconds(5)))
				.ToArray();
			int attempt = 1;
			while(true)
			{
				var succeeded = await managers.AnyAsync(manager => operation(manager, attempt++));
				if (succeeded || maxAttempts < attempt)
				{
					break;
				}
				await Task.Delay(500);
			}
		}

		
	}
}
