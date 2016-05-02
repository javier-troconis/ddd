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

        class Handler1 : IMessageHandler<int, string>
        {
            public string Handle(int message)
            {
                return message.ToString();
            }
        }

        class Handler2 : IMessageHandler<string, int>
        {
            public int Handle(string message)
            {
                return int.Parse(message);
            }
        }

        public static void Main(string[] args)
		{
            Console.WriteLine(new Handler1().ComposeForward(new Handler2()).ComposeForward(new Handler1()).Handle(1));
            Console.WriteLine(new Handler1().ComposeBackwards(new Handler2()).ComposeBackwards(new Handler1()).Handle(1));

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
