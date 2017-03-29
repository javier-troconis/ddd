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
			{
				var streamName = "application-" + Guid.NewGuid().ToString("N").ToLower();
				
				// start application
				eventStore.WriteEvents(streamName, ExpectedVersion.NoStream, Commands.StartApplicationV2()).Wait();
				Console.WriteLine("application started: " + streamName);


				// submit application
				var readEventsForwardTask = eventStore.ReadEventsForward(streamName);
	            readEventsForwardTask.Wait();
	            var events = readEventsForwardTask.Result;
	            var state = events.FoldOver(new SubmitApplicationState());
				var newEvents = Commands.SubmitApplicationV1(state, streamName);
				OptimisticEventWriter.WriteEvents(ConflictResolutionStrategy.SkipConflicts, eventStore, streamName, ExpectedVersion.NoStream, newEvents).Wait();
				Console.WriteLine("application submitted: " + streamName);

				Task.Delay(2000).Wait();

			}
        }

	    


	}
}
