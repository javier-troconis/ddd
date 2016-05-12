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


    class UpcastMessageHandler<TIn, TOut> : IMessageHandler<TIn, TOut> where TIn : TOut
    {
        public TOut Handle(TIn message)
        {
            return message;
        }
    }


    class DowncastMessageHandler<TIn, TOut> : IMessageHandler<TIn, TOut> where TOut : TIn
    {
        public TOut Handle(TIn message)
        {
            return (TOut)message;
        }
    }


    class SetCommandHeaderHandler : IMessageHandler<IHeader<CommandHeader>, IHeader<CommandHeader>>
    {
        public IHeader<CommandHeader> Handle(IHeader<CommandHeader> message)
        {
            message.Header = new CommandHeader { TenantId = Guid.NewGuid() };
            return message;
        }
    }

    class AuthenticateHandler : IMessageHandler<IHeader<CommandHeader>, IHeader<CommandHeader>>
    {
        public IHeader<CommandHeader> Handle(IHeader<CommandHeader> message)
        {
            return message;
        }
    }

    class StartApplicationCommandHandler : IMessageHandler<Message<CommandHeader, StartApplicationCommand>, Task<Tuple<string, IEnumerable<IEvent>>>>
    {
        private readonly IEventStore _eventStore;

        public StartApplicationCommandHandler(IEventStore eventStore)
        {
            _eventStore = eventStore;
        }

        public async Task<Tuple<string, IEnumerable<IEvent>>> Handle(Message<CommandHeader, StartApplicationCommand> message)
        {
            var applicationId = "application-" + StreamNamingConvention.From(message.Body.ApplicationId);
            var newChanges = ApplicationAction.Start();
            await _eventStore.WriteEventsAsync(applicationId, ExpectedVersion.NoStream, newChanges);
            return new Tuple<string, IEnumerable<IEvent>>(applicationId, newChanges);
        }
    }


    public class Program
	{
		private static readonly IEventStore EventStore;

		static Program()
		{
            var eventStoreConnection = EventStoreConnectionFactory.Create(x => x.KeepReconnecting().SetOperationTimeoutTo(TimeSpan.FromSeconds(1)));

            eventStoreConnection.Disconnected += (s, a) =>
            {
                Console.WriteLine("disconnected");
            };

            eventStoreConnection.Closed += (s, a) =>
            {
                Console.WriteLine("closed");
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

            var middleware = new UpcastMessageHandler<Message<CommandHeader, StartApplicationCommand>, IHeader<CommandHeader>>()
               .ComposeForward(new SetCommandHeaderHandler())
               .ComposeForward(new AuthenticateHandler())
               .ComposeForward(new DowncastMessageHandler<IHeader<CommandHeader>, Message<CommandHeader, StartApplicationCommand>>());

            var startApplication = new StartApplicationCommandHandler(EventStore);

            startApplication.ComposeBackward(middleware)
                .Handle(new Message<CommandHeader, StartApplicationCommand> { Body = new StartApplicationCommand { ApplicationId = Guid.NewGuid() } }).Wait();

            //while (true)
            //{
            //    //run application scenarios
            //    var applicationId = "application-" + StreamNamingConvention.From(Guid.NewGuid());
            //    RunSequence
            //    (
            //        StartApplication(applicationId),
            //        SubmitApplication(applicationId, 0, "rich hickey")
            //    ).Wait();
            //}
            
        }

		static async Task RunSequence(params Func<Task>[] actions)
		{
			foreach (var action in actions)
			{
                try
                {
                    await action();
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }
			}
		}
		
		static Func<Task> StartApplication(string applicationId)
		{
			return async () =>
			{
                Console.WriteLine("starting application: " + applicationId);
                var newChanges = ApplicationAction.Start();
                await EventStore.WriteEventsAsync(applicationId, ExpectedVersion.NoStream, newChanges);
			};
		}

		static Func<Task> SubmitApplication(string applicationId, int version, string submitter)
		{
			return async () =>
			{
                Console.WriteLine("submitting application: " + applicationId);
                var currentChanges = await EventStore.ReadEventsAsync(applicationId);
                var currentState = currentChanges.Aggregate(new WhenSubmittingApplicationState(), StreamStateFolder.Fold);
				var newChanges = ApplicationAction.Submit(currentState, submitter);
                await OptimisticEventWriter.WriteEventsAsync(StreamVersionConflictResolution.AlwaysCommit, EventStore, applicationId, version, newChanges);
			};
		}

        
	}
}
