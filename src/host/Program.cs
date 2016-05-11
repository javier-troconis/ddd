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


    class UpcastHandler<TIn, TOut> : IMessageHandler<TIn, TOut> where TIn : TOut
    {
        public TOut Handle(TIn message)
        {
            return message;
        }
    }


    class DowncastHandler<TIn, TOut> : IMessageHandler<TIn, TOut> where TOut : TIn
    {
        public TOut Handle(TIn message)
        {
            return (TOut)message;
        }
    }


    class FillHeaderHandler : IMessageHandler<IHeader, IHeader>
    {
        public IHeader Handle(IHeader message)
        {
            message.Header = new Header();
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

    class StartApplicationCommandHandler : IMessageHandler<Message<StartApplicationCommand>, IEnumerable<IEvent>>
    {
        public IEnumerable<IEvent> Handle(Message<StartApplicationCommand> message)
        {
            throw new NotImplementedException();
        }
    }

    public class Program
	{
		private static readonly IEventStore EventStore;

		static Program()
		{
            var eventStoreConnection = EventStoreConnectionFactory.Create(x => x.LimitReconnectionsTo(5));

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

            var requestMiddleware = new FillHeaderHandler()
                .ComposeForward(new AuthenticateHandler());

            //composing handlers

            var startApplication = new UpcastHandler<Message<StartApplicationCommand>, IHeader>()
                .ComposeForward(requestMiddleware)
                .ComposeForward(new DowncastHandler<IHeader, Message<StartApplicationCommand>>())
                .ComposeForward(new StartApplicationCommandHandler());

            startApplication.Handle(new Message<StartApplicationCommand>());

            //run application scenarios
            var applicationId = "application-" + StreamNamingConvention.From(Guid.NewGuid());
            RunSequence
            (
                StartApplication(applicationId),
                SubmitApplication(applicationId, 0, "rich hickey")
            ).Wait();
            
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
