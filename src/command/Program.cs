using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

using EventStore.ClientAPI;

using eventstore;

using shared;

namespace command
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var connectionFactory = new EventStoreConnectionFactory(
				EventStoreSettings.ClusterDns,
				EventStoreSettings.InternalHttpPort,
				EventStoreSettings.Username,
				EventStoreSettings.Password);
			var connection = connectionFactory.CreateConnection();
			connection.ConnectAsync().Wait();
			IEventStore eventStore = new eventstore.EventStore(connection);

			while (true)
			{
				Task.Run(async () =>
				{
					var streamName = "application-" + Guid.NewGuid().ToString("N").ToLower();
					eventStore.WriteEvents(streamName, ExpectedVersion.NoStream, Commands.StartApplicationV2()).Wait();
					Console.WriteLine("application started: " + streamName);

					var events = await eventStore.ReadEventsForward(streamName);
					var state = events.Aggregate(new SubmitApplicationV1State(), MessageHandlerExtensions.Apply);
					var newEvents = Commands.SubmitApplicationV1(state, streamName);
					await OptimisticEventWriter.WriteEvents(eventStore, streamName, ExpectedVersion.NoStream, newEvents, ConflictResolutionStrategy.IgnoreConflicts);
					Console.WriteLine("application submitted: " + streamName);

					await Task.Delay(5000);
				}).Wait();
			}
		}
	}
}
