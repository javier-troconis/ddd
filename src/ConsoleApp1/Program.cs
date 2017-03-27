using System;
using System.Net;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Common.Log;
using EventStore.ClientAPI.Exceptions;
using EventStore.ClientAPI.Projections;
using EventStore.ClientAPI.SystemData;

namespace ConsoleApp1
{
	public class Program
	{
		public static void Main(string[] args)
		{
			// create projection. only one attempt to create the projection succeeds
			Parallel.For(0, 9, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, async x =>
			{
				var manager = new ProjectionsManager(new ConsoleLogger(), new IPEndPoint(IPAddress.Loopback, 1114), TimeSpan.FromSeconds(5));
				try
				{
					await manager.CreateContinuousAsync("x", "fromAll().when({$any: function(s, e){ linkTo('x', e);}});", true, new UserCredentials("admin", "changeit"));
				}
				catch (ProjectionCommandFailedException)
				{
				}
			});

			// write an event
			var connection = EventStoreConnection.Create(
				ConnectionSettings.Create()
					.SetDefaultUserCredentials(new UserCredentials("admin", "changeit")),
				ClusterSettings.Create()
					.DiscoverClusterViaDns()
					.SetMaxDiscoverAttempts(int.MaxValue)
					.SetClusterDns("fake.dns")
					.SetClusterGossipPort(1113)
				);
			connection.ConnectAsync().Wait();
			connection.AppendToStreamAsync("y", ExpectedVersion.Any, new EventData(Guid.NewGuid(), "y", true, new byte[0], new byte[0])).Wait();

			// kill all the nodes and restart the cluster. projection x shows up as faulted
		}
	}
}
