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
					var state = new SubmitApplicationState();
					
					//var state = events.Aggregate(
					//	new SubmitApplicationState().CreateResolvedEventHandler(),
					//	(state, @event) =>
					//	{
							
					//	});
					var newEvents = Command.SubmitApplicationV1(state, streamName);
					await OptimisticEventWriter.WriteEvents(eventStore, streamName, ExpectedVersion.NoStream, newEvents, ConflictResolutionStrategy.IgnoreConflicts);
					Console.WriteLine("application submitted: " + streamName);

					await Task.Delay(1000);
				}).Wait();
			}
		}
    }
}
