using infra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Common.Log;
using EventStore.ClientAPI.Exceptions;
using EventStore.ClientAPI.Projections;
using EventStore.ClientAPI.SystemData;

namespace subscriber
{
	public class Program
	{
		private static readonly IEventStoreConnection Connection = EventStoreConnectionFactory.Create(x => x.KeepReconnecting());
		private static readonly ProjectionsManager ProjectionsManager = new ProjectionsManager(new ConsoleLogger(), Settings.HttpEndPoint, TimeSpan.FromMilliseconds(5000));
		private const string SubscriptionGroupName = "application";
		private static readonly string[] AvailableEventTypes = {"applicationstarted", "applicationsubmitted"};
		private static readonly Random Random = new Random();

		public static void Main(string[] args)
		{
			Connection.Disconnected += (s, a) =>
			{
				Console.WriteLine("disconnected");
			};

			Connection.Closed += (s, a) =>
			{
				Console.WriteLine("closed");
			};

			Connection.Connected += (s, a) =>
			{
				Console.WriteLine("connected");
			};

			Connection.Reconnecting += (s, a) =>
			{
				Console.WriteLine("reconnecting");
			};

			Connection.ConnectAsync().Wait();

			Subscribe();

			while (true)
			{
				Console.ReadLine();
				//Task.Delay(1000)
				//	.ContinueWith(delegate 
				//	{
						CreateSubscriberGroup();
				//	}).Wait();
			}
		}

		public static void CreateSubscriberGroup()
		{
			var eventTypes = AvailableEventTypes.Take(Random.Next(1, AvailableEventTypes.Length + 1)).ToArray();
			
			Console.WriteLine("listening to: " + string.Join(", ", eventTypes));

			const string onEventReceivedStatementTemplate = "'{0}': onEventReceived";
			const string projectionDefinitionTemplate =
				@"var onEventReceived = function(s, e) {{ " +
					"linkTo('{0}', e); " +
				"}};" +
				"fromAll().when({{ {1} }});";

			var projectionDefinition = string.Format(projectionDefinitionTemplate, SubscriptionGroupName, 
				string.Join(",", eventTypes.Select(eventType => string.Format(onEventReceivedStatementTemplate, eventType))));


			CreateOrUpdateProjection(SubscriptionGroupName, projectionDefinition);

			var subcriptionGroupSettingsBuilder = PersistentSubscriptionSettings.Create()
					.ResolveLinkTos()
					.StartFromCurrent();
			var subcriptionGroupSettings = subcriptionGroupSettingsBuilder.Build();

			Connection.CreatePersistentSubscriptionAsync(SubscriptionGroupName, SubscriptionGroupName, subcriptionGroupSettings, Settings.Credentials);
		}

		public static void Subscribe()
		{
			Connection.ConnectToPersistentSubscription(SubscriptionGroupName, SubscriptionGroupName, 
				(s, e) =>
					{
						Console.WriteLine($"{e.OriginalEventNumber} - {e.Event.EventType} - {e.Event.EventId}");
						s.Acknowledge(e);
					},
				(s,r,e) => 
					{
						Subscribe();
						Console.WriteLine("subscription dropped");
					});
		}

		private static void CreateOrUpdateProjection(string projectionName, string projectionDefinition)
		{
			if (TryCreateProjection(projectionName, projectionDefinition))
			{
				return;
			}
			UpdateProjection(projectionName, projectionDefinition);
		}

		private static bool TryCreateProjection(string projectionName, string projectionDefinition)
		{
			try
			{
				ProjectionsManager.CreateContinuousAsync(projectionName, projectionDefinition, Settings.Credentials).Wait();
				return true;
			}
			catch (AggregateException ex)
			{
				if (ex.InnerExceptions.Count > 1 || !(ex.InnerException is ProjectionCommandConflictException))
				{
					throw;
				}
			}
			return false;
		}

		private static void UpdateProjection(string projectionName, string projectionDefinition)
		{
			ProjectionsManager.UpdateQueryAsync(projectionName, projectionDefinition, Settings.Credentials).Wait();
		}
	}
}
