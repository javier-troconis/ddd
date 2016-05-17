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

    


    class TimedTaskHandler<TIn, TOut> : IMessageHandler<Task<TIn>, Task<TOut>> where TIn : TOut
    {
        private readonly TimeSpan _timeout;

        public TimedTaskHandler(TimeSpan timeout)
        {
            _timeout = timeout;
        }

        public async Task<TOut> Handle(Task<TIn> message)
        {
            return await message.TimeoutAfter(_timeout);
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
            var startApplicationHandler = new AuthorizeHandler<Message<StartApplicationCommand>, Message<StartApplicationCommand>>()
                .ComposeForward(new StartApplicationCommandHandler(EventStore))
                .ComposeForward(new TimedTaskHandler<Message<StartApplicationCommand>, Message<StartApplicationCommand>>(TimeSpan.FromSeconds(2)))
                .ComposeForward(new TaskCompletedLoggerHandler<Message<StartApplicationCommand>, Message<StartApplicationCommand>>(Console.WriteLine, message => $"application {message.Body.ApplicationId}: started"));

            var submitApplicationHandler = new AuthorizeHandler<Message<SubmitApplicationCommand>, Message<SubmitApplicationCommand>>()
                .ComposeForward(new SubmitApplicationCommandHandler(EventStore))
                .ComposeForward(new TimedTaskHandler<Message<SubmitApplicationCommand>, Message<SubmitApplicationCommand>>(TimeSpan.FromSeconds(4)))
                .ComposeForward(new TaskCompletedLoggerHandler<Message<SubmitApplicationCommand>, Message<SubmitApplicationCommand>>(Console.WriteLine, message => $"application {message.Body.ApplicationId}: submitted"));

            while (true)
            {
                var applicationId = Guid.NewGuid();
                try
                {
                    startApplicationHandler.Handle(new Message<StartApplicationCommand> { Body = new StartApplicationCommand { ApplicationId = applicationId } }).Wait();
                    submitApplicationHandler.Handle(new Message<SubmitApplicationCommand> { Body = new SubmitApplicationCommand { ApplicationId = applicationId, Submitter = "rich hickey", Version = 0 } }).Wait();
                }
                catch (AggregateException ex)
                {
                    Console.WriteLine(ex.InnerException.Message);
                }
                Task.Delay(2000).Wait();
            }
        }

    }
}
