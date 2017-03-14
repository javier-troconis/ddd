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
			var connection = new EventStoreConnectionFactory(x => x
                .KeepReconnecting().SetDefaultUserCredentials(new UserCredentials(EventStoreSettings.Username, EventStoreSettings.Password))).CreateConnection();
            connection.ConnectAsync().Wait();
            IEventStore eventStore = new infra.EventStore(connection);

            while (true)
            {
				var streamName = "application-" + NamingConvention.Stream(Guid.NewGuid());
				eventStore.WriteEvents(streamName, ExpectedVersion.NoStream, Commands.StartApplicationV1()).Wait();

				var readEventsForwardTask = eventStore.ReadEventsForward(streamName);
	            readEventsForwardTask.Wait();
	            var events = readEventsForwardTask.Result;
	            var state = events.FoldOver(new SubmitApplicationState());
				var newEvents = Commands.SubmitApplicationV1(state, "xxx");
				OptimisticEventWriter.WriteEvents(ConflictResolutionStrategy.SkipConflicts, eventStore, streamName, ExpectedVersion.NoStream, newEvents).Wait();

				Task.Delay(TimeSpan.FromSeconds(1)).Wait();
            }
        }

	    


	}
}
