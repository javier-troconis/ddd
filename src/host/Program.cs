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
    

    public class Program
	{
		private static readonly IEventStore EventStore;

		static Program()
		{
            var eventStoreConnection = EventStoreConnectionFactory.Create(x => x.KeepReconnecting());
            eventStoreConnection.ConnectAsync().Wait();
            EventStore = new infra.EventStore(eventStoreConnection);
        }

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

        public static void Main(string[] args)
		{

            var handler = new HandlerA().ComposeForward(new HandlerB()).ComposeForward(new HandlerC()).ComposeBackwards(new HandlerC()).ComposeBackwards(new HandlerB()).ComposeBackwards(new HandlerA());

            Enumerable.Range(0, 10).Select(x => handler.Handle(Guid.NewGuid())).ToArray();


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
				var currentState = currentChanges.Aggregate(new WhenSubmittingApplicationState(), EventFolder.Fold);
				var newChanges = ApplicationAction.Submit(currentState, submitter);
                //await OptimisticEventWriter.WriteEventsAsync(OptimisticEventWriter.AlwaysCommit, EventStore, applicationId, version, newChanges);
                await EventStore.WriteEventsAsync(applicationId, version, newChanges);
			};
		}
	}
}
