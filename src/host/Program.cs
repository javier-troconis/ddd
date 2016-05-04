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

    class HandlerA : IMessageHandler<Guid, string>
    {
        public string Handle(Guid message)
        {
            Console.WriteLine($"HandlerA called : {message}");
            return "application-" + StreamNamingConvention.From(message);
        }
    }

    class HandlerB : IMessageHandler<string, Guid>
    {
        public Guid Handle(string message)
        {
            Console.WriteLine($"HandlerB called : {message}");
            return Guid.Parse(message.Split('-')[1]);
        }
    }

    class HandlerC : IMessageHandler<Guid, Guid>
    {
        public Guid Handle(Guid message)
        {
            Console.WriteLine($"HandlerC called : {message}");
            return message;
        }
    }

    class Handler1 : IMessageHandler<int, int>
    {
        public int Handle(int message)
        {
            Console.WriteLine($"Handler1 called : {message}");
            return message;
        }
    }

    class Handler2 : IMessageHandler<int, int>
    {
        public int Handle(int message)
        {
            Console.WriteLine($"Handler2 called : {message}");
            return message;
        }
    }
    class Handler3 : IMessageHandler<int, int>
    {
        public int Handle(int message)
        {
            Console.WriteLine($"Handler3 called : {message}");
            return message;
        }
    }

    public class Program
	{
		private static readonly IEventStore EventStore;

		static Program()
		{
            var eventStoreConnection = EventStoreConnectionFactory.Create(x => x.KeepReconnecting());
            eventStoreConnection.ConnectAsync().Wait();
            EventStore = new infra.EventStore(eventStoreConnection);
        }

        public static void Main(string[] args)
		{
            //composing handlers
            var handler = MessageHandlerComposer
                .ComposeForward(new Handler1(), new Handler2())
                .ComposeBackward(new Handler3())
                .ComposeBackward(new Handler1());
            Console.WriteLine(handler.Handle(1));

            //composing handlers
            var handler1 = MessageHandlerComposer
                .ComposeBackward(new HandlerA(), new HandlerB())
                .ComposeBackward(new HandlerA())
                .ComposeBackward(new HandlerC())
                .ComposeForward(new HandlerB());
            Console.WriteLine(handler1.Handle(Guid.NewGuid()));

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
                var newChanges = ApplicationAction.Start();
                await EventStore.WriteEventsAsync(applicationId, ExpectedVersion.NoStream, newChanges);
			};
		}

		static Func<Task> SubmitApplication(string applicationId, int version, string submitter)
		{
			return async () =>
			{
                var currentChanges = await EventStore.ReadEventsAsync(applicationId);
				var currentState = currentChanges.Aggregate(new WhenSubmittingApplicationState(), EventDispatcher.Dispatch);
				var newChanges = ApplicationAction.Submit(currentState, submitter);
                await OptimisticEventWriter.WriteEventsAsync(StreamVersionConflictResolution.AlwaysCommit, EventStore, applicationId, version, newChanges);
			};
		}
	}
}
