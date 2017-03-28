using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using core;

using eventstore;

using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;

using Newtonsoft.Json;

using shared;

namespace host
{
    

    public class Program
    {
        public static void Main(string[] args)
        {
	        var connectionFactory = new EventStoreConnectionFactory(EventStoreSettings.ClusterDns, EventStoreSettings.InternalHttpPort);
	        var connection = connectionFactory.CreateConnection();
			connection.ConnectAsync().Wait();
			IEventStore eventStore = new eventstore.EventStore(connection);

			while (true)
			{
				var streamName = "application-" + Guid.NewGuid().ToString("N").ToLower();
				
				// start application
				eventStore.WriteEvents(streamName, ExpectedVersion.NoStream, Commands.StartApplicationV1()).Wait();
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
