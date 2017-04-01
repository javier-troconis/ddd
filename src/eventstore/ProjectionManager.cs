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
		Task CreateOrUpdateContinuousProjection(string name, string query);
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

		public async Task CreateOrUpdateContinuousProjection(string name, string query)
		{
			try
			{
				await CreateContinuousProjection(name, query);
			}
			catch (ProjectionCommandConflictException)
			{
				var storedQuery = await GetProjectionQuery(name);
                if(!string.Equals(query, storedQuery))
                {
                    await UpdateProjection(name, query);
                }
			} 
		}

		private async Task<string> GetProjectionQuery(string name)
		{
			string query = string.Empty;
			await Execute(_clusterDns, _externalHttpPort, _logger, async (manager, maxAttempts, attempt) =>
			{
				try
				{
					query = await manager.GetQueryAsync(name, new UserCredentials(_username, _password));
				}
				catch (ProjectionCommandFailedException ex) when (ex.HttpStatusCode == (int)HttpStatusCode.NotFound)
				{
					throw;
				}
                catch (Exception) when (maxAttempts >= attempt + 1)
                {
					return false;
				}
				_logger.Info("Projection {0} query retrieved.", name);
				return true;
			});
			return query;
		}

		private Task CreateContinuousProjection(string name, string query)
		{
			return Execute(_clusterDns, _externalHttpPort, _logger, async (manager, maxAttempts, attempt) =>
			{
				try
				{
					await manager.CreateContinuousAsync(name, query, true, new UserCredentials(_username, _password));
				}
				catch (ProjectionCommandConflictException)
				{
					throw;
				}
                catch (Exception) when (maxAttempts >= attempt + 1)
                {
					return false;
				}
				_logger.Info("Projection {0} created.", name);
				return true;
			});
		}

		private Task UpdateProjection(string name, string newQuery)
		{
			return Execute(_clusterDns, _externalHttpPort, _logger, async (manager, maxAttempts, attempt) =>
			{
				try
				{
					await manager.UpdateQueryAsync(name, newQuery, new UserCredentials(_username, _password));
				}
				catch (ProjectionCommandConflictException)
				{
					throw;
				}
				catch (Exception) when (maxAttempts >= attempt + 1)
				{
					return false;
				}
				_logger.Info("Projection {0} updated.", name);
				return true;
			});
		}

		private static Task Execute(string clusterDns, int externalHttpPort, ILogger logger, Func<ProjectionsManager, int, int, Task<bool>> operation)
		{
			var httpEndpoints = Dns.GetHostEntry(clusterDns)
				.AddressList
				.Select(ipAddress => 
					new IPEndPoint(ipAddress, externalHttpPort))
                .ToArray();
            return httpEndpoints
                .AnyAsync((httpEndpoint, index) => 
                    operation(new ProjectionsManager(logger, httpEndpoint, TimeSpan.FromSeconds(5)), httpEndpoints.Length, index + 1));
        }

		
	}
}
