using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using appservices;
using core;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using infra;
using shared;

namespace host
{
    class SetMessageHeaderHandler<TIn, TOut> : IMessageHandler<TIn, TOut> where TIn : IHeader, TOut
    {
        public TOut Handle(TIn message)
        {
            message.Header = new Dictionary<string, object>();
            return message;
        }
    }

    class AuthorizeHandler<TIn, TOut> : IMessageHandler<TIn, TOut> where TIn : IHeader, TOut
    {
        public TOut Handle(TIn message)
        {
            return message;
        }
    }

    class TimeFramedTaskHandler<TIn, TOut> : IMessageHandler<Task<TIn>, Task<TOut>> where TIn : TOut
    {
        private readonly TimeSpan _timeout;
       
        public TimeFramedTaskHandler(TimeSpan timeout)
        {
            _timeout = timeout;
        }

        public async Task<TOut> Handle(Task<TIn> message)
        {   var cancellationTokenSource = new CancellationTokenSource(_timeout);
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

    class EventWriterHandler<TIn, TOut> : IMessageHandler<IEnumerable<IEvent>, Task>
    {
        private readonly IEventStore _eventStore;
        private readonly string _streamPrefix;

        public EventWriterHandler(IEventStore eventStore, string streamPrefix)
        {
            _eventStore = eventStore;
            _streamPrefix = streamPrefix;
        }

        public Task Handle(IEnumerable<IEvent> message)
        {
            //await OptimisticEventWriter.WriteEventsAsync(ConflictResolutionType.IgnoreConflict, _eventStore, applicationId, message.Body.Version, newChanges);
            throw new NotImplementedException();
        }
    }

    public class Program
    {
        private static readonly IEventStore EventStore;

        static Program()
        {
            var eventStoreConnection = EventStoreConnectionFactory.Create(x => x.KeepReconnecting());

            eventStoreConnection.Disconnected += (s, a) =>
            {
                Console.WriteLine("disconnected");
            };

            eventStoreConnection.Closed += (s, a) =>
            {
                Console.WriteLine("closed");
            };

            eventStoreConnection.ErrorOccurred += (s, a) =>
            {
                Console.WriteLine("errorocurred" + a.Exception);
            };

            eventStoreConnection.Connected += (s, a) =>
            {
                Console.WriteLine("connected");
            };

            eventStoreConnection.Reconnecting += (s, a) =>
            {
                Console.WriteLine("reconnecting");
            };
            eventStoreConnection.ConnectAsync().Wait();

            EventStore = new infra.EventStore(eventStoreConnection);
        }

        

        public static void Main(string[] args)
        {

            //Console.WriteLine(new Func<int, int>(x => x).ComposeForward(new Func<int, string>(x => x.ToString()).ComposeForward(x => x)).ComposeForward(x => x + 1)(1));


            Console.WriteLine(new Func<int, int>(x => x).ComposeBackward<int, int, int>(x => x).ComposeBackward<int, int, int>(x => x));

            //var startApplicationHandler = new StartApplicationCommandHandler().ComposeForward(new EventWriterHandler(EventStore, "application"))

            //    .ComposeForward(new TimeFramedTaskHandler<Message<StartApplicationCommand>, Message<StartApplicationCommand>>(TimeSpan.FromSeconds(2)))
            //    .ComposeForward(new TaskCompletedLoggerHandler<Message<StartApplicationCommand>, Message<StartApplicationCommand>>(Console.WriteLine, message => $"application {message.Body.ApplicationId}: started"));

            //var submitApplicationHandler = new SubmitApplicationCommandHandler(EventStore)
            //    .ComposeForward(new TimeFramedTaskHandler<Message<SubmitApplicationCommand>, Message<SubmitApplicationCommand>>(TimeSpan.FromSeconds(2)))
            //    .ComposeForward(new TaskCompletedLoggerHandler<Message<SubmitApplicationCommand>, Message<SubmitApplicationCommand>>(Console.WriteLine, message => $"application {message.Body.ApplicationId}: submitted"));

            while (true)
            {
                var applicationId = Guid.NewGuid();
                try
                {
                    //startApplicationHandler.Handle(new Message<StartApplicationCommand> { Body = new StartApplicationCommand { ApplicationId = applicationId } }).Wait();
                    //submitApplicationHandler.Handle(new Message<SubmitApplicationCommand> { Body = new SubmitApplicationCommand { ApplicationId = applicationId, Version = -1, Submitter = "javier" } }).Wait();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.InnerException.Message);
                }
                Task.Delay(1000).Wait();
            }
        }

    }
}
