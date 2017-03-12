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
    class TimeFramedTaskHandler<TIn, TOut> : IMessageHandler<Task<TIn>, Task<TOut>> where TIn : TOut
    {
        private readonly TimeSpan _timeout;

        public TimeFramedTaskHandler(TimeSpan timeout)
        {
            _timeout = timeout;
        }

        public async Task<TOut> Handle(Task<TIn> message)
        {
            var cancellationTokenSource = new CancellationTokenSource(_timeout);
            return await message.WithCancellation(cancellationTokenSource.Token);
        }
    }

    class TaskCompletedLoggerHandler<TIn, TOut> : IMessageHandler<Task<TIn>, Task<TOut>> where TIn : TOut
    {
        private readonly Action<string> _logger;
        private readonly Func<TOut, string> _whatToLog;

        public TaskCompletedLoggerHandler(Action<string> logger, Func<TOut, string> whatToLog)
        {
            _logger = logger;
            _whatToLog = whatToLog;
        }

        public async Task<TOut> Handle(Task<TIn> message)
        {
            var @out = await message;
            _logger(_whatToLog(@out));
            return @out;
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
			var connection = new EventStoreConnectionFactory(x => x
                .KeepReconnecting().SetDefaultUserCredentials(new UserCredentials(EventStoreSettings.Username, EventStoreSettings.Password))).CreateConnection();
            connection.ConnectAsync().Wait();
            IEventStore eventStore = new infra.EventStore(connection);

            //var startApplicationHandler = new StartApplicationCommandHandler(eventStore)
            //    .ComposeForward(new TimeFramedTaskHandler<Message<StartApplicationCommand>, Message<StartApplicationCommand>>(TimeSpan.FromSeconds(2)))
            //    .ComposeForward(new TaskCompletedLoggerHandler<Message<StartApplicationCommand>, Message<StartApplicationCommand>>(Console.WriteLine, message => $"application {message.Body.ApplicationId}: started"));

            //var submitApplicationHandler = new SubmitApplicationCommandHandler(eventStore)
            //    .ComposeForward(new TimeFramedTaskHandler<Message<SubmitApplicationCommand>, Message<SubmitApplicationCommand>>(TimeSpan.FromSeconds(2)))
            //    .ComposeForward(new TaskCompletedLoggerHandler<Message<SubmitApplicationCommand>, Message<SubmitApplicationCommand>>(Console.WriteLine, message => $"application {message.Body.ApplicationId}: submitted"));

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

				OptimisticEventWriter.WriteEvents(ConflictResolutionStrategy.SkipConflicts, eventStore, streamName, -1, events).Wait();

				Task.Delay(TimeSpan.FromSeconds(1)).Wait();
            }
        }

    
      
        
    }
}
