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
            while (true)
            {
                Task.Run(async () =>
                {
                    var connectionFactory = new EventStoreConnectionFactory(
                        EventStoreSettings.ClusterDns,
                        EventStoreSettings.InternalHttpPort,
                        EventStoreSettings.Username,
                        EventStoreSettings.Password);
                    using (var connection = connectionFactory.CreateConnection())
                    {
                        await connection.ConnectAsync();

                        IEventStore eventStore = new eventstore.EventStore(connection);

                        var streamName = "application-" + Guid.NewGuid().ToString("N").ToLower();
                        eventStore.WriteEvents(streamName, ExpectedVersion.NoStream, Commands.StartApplicationV2()).Wait();
                        Console.WriteLine("application started: " + streamName);

                        var events = await eventStore.ReadEventsForward(streamName);
                        var state = events.Aggregate(new SubmitApplicationV1State(), MessageHandlerExtensions.Apply);
                        var newEvents = Commands.SubmitApplicationV1(state, streamName);
                        await OptimisticEventWriter.WriteEvents(eventStore, streamName, ExpectedVersion.NoStream, newEvents, ConflictResolutionStrategy.IgnoreConflicts);
                        Console.WriteLine("application submitted: " + streamName);
                    }
                    await Task.Delay(1000);
                }).Wait();
            }
        }
    }
}
