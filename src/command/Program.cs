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
				EventStoreSettings.Password,
				x => x
					.WithConnectionTimeoutOf(TimeSpan.FromMinutes(1)));
			var connection = connectionFactory.CreateConnection();
			connection.ConnectAsync().Wait();
			IEventStore eventStore = new eventstore.EventStore(connection);

			//while (true)
			//{
			//    Console.WriteLine("1 - IApplicationStartedV1");
			//    Console.WriteLine("2 - IApplicationStartedV2");
			//    Console.WriteLine("3 - IApplicationStartedV3");

			//    var option = Console.ReadKey().KeyChar;
			//    switch (option)
			//    {
			//        case '1':
			//            eventStore.WriteEvents(
			//                 "application-" + Guid.NewGuid().ToString("N").ToLower(),
			//                 ExpectedVersion.NoStream,
			//                 Command.StartApplicationV1())
			//             .Wait();
			//            break;
			//        case '2':
			//            eventStore.WriteEvents(
			//                 "application-" + Guid.NewGuid().ToString("N").ToLower(),
			//                 ExpectedVersion.NoStream,
			//                 Command.StartApplicationV2())
			//             .Wait();
			//            break;
			//        case '3':
			//            eventStore.WriteEvents(
			//                "application-" + Guid.NewGuid().ToString("N").ToLower(),
			//                ExpectedVersion.NoStream,
			//                Command.StartApplicationV3())
			//                .Wait();
			//            break;
			//        default:
			//            return;
			//    }
			//    Console.WriteLine();
			//}

			while (true)
			{
				Task.Run(async () =>
				{
					var streamName = "application-" + Guid.NewGuid().ToString("N").ToLower();
					await eventStore.WriteEvents(streamName, ExpectedVersion.NoStream, Command.StartApplicationV3());
					Console.WriteLine("application started: " + streamName);

					var events = await eventStore.ReadEventsForward(streamName);

					var state = events
					  .Aggregate(
						  new SubmitApplicationState(),
						  (x, y) =>
						  {
							  var z = x.CreateResolvedEventHandle(resolvedEvent => default(SubmitApplicationState));
							  var result = z(y);
							  return Equals(result, default(SubmitApplicationState)) ? x : result;
						  });

					var newEvents = Command.SubmitApplicationV1(state, streamName);
					await eventStore.WriteEvents(streamName, 0, newEvents);
					//await OptimisticEventWriter.WriteEvents(eventStore, streamName, ExpectedVersion.NoStream, newEvents, ConflictResolutionStrategy.IgnoreConflicts);
					Console.WriteLine("application submitted: " + streamName);

					await Task.Delay(1000);
				}).Wait();
			}
		}
	}
}
