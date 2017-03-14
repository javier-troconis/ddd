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
                var applicationId = Guid.NewGuid();
				var streamName = "application-" + NamingConvention.Stream(applicationId);
	            var events = Commands.StartApplicationV1()
					.Concat(Commands.StartApplicationV2())
					.Concat(Commands.StartApplicationV3());
				eventStore.WriteEvents(streamName, ExpectedVersion.NoStream, events).Wait();

				var readEventsForwardTask = eventStore.ReadEventsForward(streamName);
	            readEventsForwardTask.Wait();
	            events = readEventsForwardTask.Result;

				events = Commands.SubmitApplicationV1(events.FoldOver(new SubmitApplicationState()), "xxx");

				OptimisticEventWriter.WriteEvents(ConflictResolutionStrategy.SkipConflicts, eventStore, streamName, ExpectedVersion.NoStream, events).Wait();

				Task.Delay(TimeSpan.FromSeconds(1)).Wait();
            }
        }

    
      
        
    }
}
