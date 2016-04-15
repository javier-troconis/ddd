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

		public static void Main(string[] args)
		{

			var applicationId = StreamNamingConvention.FromIdentity(Guid.NewGuid());

			RunSequence
			(
				StartApplication(applicationId),
				SubmitApplication(applicationId, 0)
			).Wait();
		}

		static async Task RunSequence(params Func<Task>[] actions)
		{
			foreach(var action in actions)
			{
				try
				{
					await action();
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.Message);
				}
			}
		}

		static Func<Task> StartApplication(string applicationId)
		{
			return async () =>
			{
				var events = Application.Start();
				await EventStore.WriteEventsAsync(applicationId, ExpectedVersion.NoStream, events);
			};
		}

		static Func<Task> SubmitApplication(string applicationId, int version)
		{
			return async () =>
			{
				var state = new Application.SubmitState();
				await StreamReader.ReadAsync(EventStore.ReadEventsAsync, applicationId, state);

				var events = Application.Submit(state);
				await EventStore.WriteEventsAsync(applicationId, version, events);
			};
		}
	}
}
