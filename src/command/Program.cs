using System;
using System.Threading.Tasks;

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
		        Task.Run(async () =>
		        {
					var streamName = "application-" + Guid.NewGuid().ToString("N").ToLower();

					// start application
					await eventStore.WriteEvents(streamName, ExpectedVersion.NoStream, Commands.StartApplicationV2());
					Console.WriteLine("application started: " + streamName);

					// submit application
					var state = (await eventStore.ReadEventsForward(streamName)).FoldOver(new SubmitApplicationState());
					var events = Commands.SubmitApplicationV1(state, streamName);
					await OptimisticEventWriter.WriteEvents(ConflictResolutionStrategy.SkipConflicts, eventStore, streamName, ExpectedVersion.NoStream, events);
					Console.WriteLine("application submitted: " + streamName);

					await Task.Delay(2000);
		        }).Wait();



        }

	    


	}
}
