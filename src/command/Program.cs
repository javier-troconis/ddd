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


            var streamName = "application-" + Guid.NewGuid().ToString("N").ToLower();
            // start application
            eventStore.WriteEvents(streamName, ExpectedVersion.NoStream, Commands.StartApplicationV2()).Wait();
            Console.WriteLine("application started: " + streamName);


            Parallel.For(1, 10, async x =>
            {
                 // submit application
                 var events = await eventStore.ReadEventsForward(streamName);
                 var state = events.Aggregate(new SubmitApplicationState(), MessageHandlerExtensions.Apply<ApplicationStartedV1, SubmitApplicationState>);
                 var newEvents = Commands.SubmitApplicationV1(state, streamName);
                 await OptimisticEventWriter.WriteEvents(eventStore, streamName, ExpectedVersion.NoStream, newEvents, ConflictResolutionStrategy.IgnoreConflicts);
                 Console.WriteLine("application submitted: " + streamName);
            });

            while (true);
        }
    }
}
