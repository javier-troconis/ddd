using System;
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
		        Task.Run(async () =>
		        {
					var streamName = "application-" + Guid.NewGuid().ToString("N").ToLower();

					// start application
					await eventStore.WriteEvents(streamName, ExpectedVersion.NoStream, Commands.StartApplicationV2());
					Console.WriteLine("application started: " + streamName);

                    // submit application
                    var state = new SubmitApplicationState().Fold<ApplicationStartedV1, SubmitApplicationState>(await eventStore.ReadEventsForward(streamName));
                    var newEvents = Commands.SubmitApplicationV1(state, streamName);
					await OptimisticEventWriter.WriteEvents(eventStore, streamName, ExpectedVersion.NoStream, newEvents, ConflictResolutionStrategy.SkipConflicts);
					Console.WriteLine("application submitted: " + streamName);

					await Task.Delay(2000);
		        }).Wait();



        }

	    


	}
}
