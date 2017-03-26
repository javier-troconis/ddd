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
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using infra;

using Newtonsoft.Json;

using shared;

namespace host
{
    

    public class Program
    {
        public static void Main(string[] args)
        {
	        var connection = EventStoreConnection.Create(ConnectionSettings.Create()
				.SetClusterDns(EventStoreSettings.ClusterDns)
				.SetClusterGossipPort(EventStoreSettings.InternalHttpPort)
				.SetDefaultUserCredentials(new UserCredentials(EventStoreSettings.Username, EventStoreSettings.Password))
				.KeepReconnecting());
	        connection.ConnectAsync().Wait();
			IEventStore eventStore = new infra.EventStore(connection);

            //while (true)
            //{
				var streamName = "application-" + Guid.NewGuid().ToString("N").ToLower();
				
				// start application
				eventStore.WriteEvents(streamName, ExpectedVersion.NoStream, Commands.StartApplicationV1()).Wait();

				// submit application
				var readEventsForwardTask = eventStore.ReadEventsForward(streamName);
	            readEventsForwardTask.Wait();
	            var events = readEventsForwardTask.Result;
	            var state = events.FoldOver(new SubmitApplicationState());
				var newEvents = Commands.SubmitApplicationV1(state, streamName);
				OptimisticEventWriter.WriteEvents(ConflictResolutionStrategy.SkipConflicts, eventStore, streamName, ExpectedVersion.NoStream, newEvents).Wait();

				Task.Delay(TimeSpan.FromSeconds(5)).Wait();
            //}
        }

	    


	}
}
