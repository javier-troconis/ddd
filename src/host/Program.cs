using System;
using System.Collections.Generic;
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
using shared;

namespace host
{
    class CastToMessageTypeHandler<TIn, TOut> : IMessageHandler<TIn, TOut> where TOut : TIn
    {
        public TOut Handle(TIn message)
        {
            return (TOut)message;
        }
    }


    class SetCommandHeaderHandler : IMessageHandler<IHeader, IHeader>
    {
        public IHeader Handle(IHeader message)
        {
            message.Header = new Dictionary<string, object>();
            return message;
        }
    }

    class AuthenticateHandler : IMessageHandler<IHeader, IHeader>
    {
        public IHeader Handle(IHeader message)
        {
            return message;
        }
    }

    class StartApplicationCommandHandler : IMessageHandler<Message<StartApplicationCommand>, Task<Message<StartApplicationCommand>>>
    {
        private readonly IEventStore _eventStore;

        public StartApplicationCommandHandler(IEventStore eventStore)
        {
            _eventStore = eventStore;
        }

        public async Task<Message<StartApplicationCommand>> Handle(Message<StartApplicationCommand> message)
        {
            var applicationId = "application-" + StreamNamingConvention.From(message.Body.ApplicationId);
            var newChanges = ApplicationAction.Start();
            await _eventStore.WriteEventsAsync(applicationId, ExpectedVersion.NoStream, newChanges);
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

    public class Program
    {
        private static readonly IEventStore EventStore;

        static Program()
        {
            var eventStoreConnection = EventStoreConnectionFactory.Create(x => x
                .KeepReconnecting()

                //.FailOnNoServerResponse()
                //.LimitAttemptsForOperationTo(1)
                //.LimitRetriesForOperationTo(1)

                );

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
            //composing handlers

            //var middleware = new SetCommandHeaderHandler()
            //   .ComposeForward(new AuthenticateHandler())
            //   .ComposeForward(new CastAsMessageHandler<IHeader, Message<StartApplicationCommand>>());

            //var startApplication = new StartApplicationCommandHandler(EventStore);

            //startApplication.ComposeBackward(middleware)
            //    .Handle(new Message<StartApplicationCommand> { Body = new StartApplicationCommand { ApplicationId = Guid.NewGuid() } }).Wait();

            var applicationNumber = 0;

            var timedTaskHandler = new TimedTaskHandler<Message<StartApplicationCommand>, Message<StartApplicationCommand>>(TimeSpan.FromSeconds(2));

            var startApplicationHandler = new StartApplicationCommandHandler(EventStore).ComposeForward(timedTaskHandler);

         
            while (true)
            {
                //run application scenarios
                //var applicationId = applicationNumber++ + "_" + StreamNamingConvention.From(Guid.NewGuid());
                //RunSequence
                //(
                //    StartApplication(applicationId),
                //    //() => Task.Delay(2000),
                //    //SubmitApplication(applicationId, 0, "rich hickey"),
                //    () => Task.Delay(2000)
                //).Wait();

           

                try
                {
                    startApplicationHandler
                        .Handle(new Message<StartApplicationCommand> { Body = new StartApplicationCommand { ApplicationId = Guid.NewGuid() } }).Wait();
                }
                catch(AggregateException ex)
                {
                    Console.WriteLine(ex.InnerException.Message);
                }

                Task.Delay(2000).Wait();
            }

        }

        static async Task RunSequence(params Func<Task>[] actions)
        {
            foreach (var action in actions)
            {
                await action();
            }
        }





        static Func<Task> StartApplication(string applicationId)
        {
            return async () =>
            {
                var newChanges = ApplicationAction.Start();
                try
                {
                    await EventStore.WriteEventsAsync(applicationId, ExpectedVersion.NoStream, newChanges).TimeoutAfter(TimeSpan.FromSeconds(2));

                    Console.WriteLine("started application: " + applicationId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"starting application: {applicationId} failed with: {ex.GetType().Name}");
                }
            };
        }

        static Func<Task> SubmitApplication(string applicationId, int version, string submitter)
        {
            return async () =>
            {
                try
                {
                    var currentChanges = await EventStore.ReadEventsAsync(applicationId).TimeoutAfter(TimeSpan.FromSeconds(2));
                    var currentState = currentChanges.Aggregate(new WhenSubmittingApplicationState(), StreamStateFolder.Fold);
                    var newChanges = ApplicationAction.Submit(currentState, submitter);
                    await OptimisticEventWriter.WriteEventsAsync(StreamVersionConflictResolution.AlwaysCommit, EventStore, applicationId, version, newChanges).TimeoutAfter(TimeSpan.FromSeconds(2));
                    Console.WriteLine("submitted application: " + applicationId);
                }
                catch (TimeoutException)
                {
                    Console.WriteLine("submmiting application timedout: " + applicationId);
                }
            };
        }


    }
}
