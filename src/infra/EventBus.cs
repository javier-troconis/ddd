using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;

using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;

using ImpromptuInterface;
using ImpromptuInterface.Dynamic;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using shared;

namespace infra
{
	public sealed class EventBus
	{
		private readonly IEnumerable<ISubscription> _subscriptions;
		private readonly Func<IEventStoreConnection> _createConnection;

		public EventBus(Func<IEventStoreConnection> createConnection)
			: this(createConnection, Enumerable.Empty<ISubscription>())
		{
		}

		private EventBus(Func<IEventStoreConnection> createConnection, IEnumerable<ISubscription> subscriptions)
		{
			_createConnection = createConnection;
			_subscriptions = subscriptions;
		}

		public EventBus RegisterCatchupSubscriber<TSubscriber>(TSubscriber subscriber, Func<Task<long?>> getCheckpoint, Func<Func<ResolvedEvent, Task<ResolvedEvent>>, Func<ResolvedEvent, Task<ResolvedEvent>>> processHandle = null)
		{
			var handle = (processHandle ?? (x => x))(resolvedEvent => HandleEvent(subscriber, resolvedEvent));
			return new EventBus(_createConnection,
				_subscriptions.Concat(new List<ISubscription>
				{
					new CatchUpSubscription(
						_createConnection,
						typeof(TSubscriber).GetEventStoreName(),
						handle,
						TimeSpan.FromSeconds(1),
						getCheckpoint)
				}));
		}

		public EventBus RegisterPersistentSubscriber<TSubscriber>(TSubscriber subscriber, Func<Func<ResolvedEvent, Task<ResolvedEvent>>, Func<ResolvedEvent, Task<ResolvedEvent>>> processHandle = null)
		{
			var handle = (processHandle ?? (x => x))(resolvedEvent => HandleEvent(subscriber, resolvedEvent));
			return new EventBus(_createConnection,
				_subscriptions.Concat(new List<ISubscription>
				{
					new PersistentSubscription(
						_createConnection,
						typeof(TSubscriber).GetEventStoreName(),
						typeof(TSubscriber).GetEventStoreName(),
						handle,
						TimeSpan.FromSeconds(1))
				}));
		}

		public Task Start()
		{
			return Task.WhenAll(_subscriptions.Select(subscription => subscription.Start()));
		}

		internal static async Task<ResolvedEvent> HandleEvent(object subscriber, ResolvedEvent resolvedEvent)
		{
			var recordedEvent = DeserializeEvent(subscriber.GetType(), resolvedEvent);
			await HandleEvent(subscriber, (dynamic)recordedEvent);
			return resolvedEvent;
		}

		internal static Task HandleEvent<TRecordedEvent>(object subscriber, TRecordedEvent recordedEvent)
		{
			var handler = (IMessageHandler<TRecordedEvent, Task>)subscriber;
			return handler.Handle(recordedEvent);
		}

		internal static object DeserializeEvent(Type subscriberType, ResolvedEvent resolvedEvent)
		{
			var eventMetadata = JsonConvert.DeserializeObject<Dictionary<string, object>>(Encoding.UTF8.GetString(resolvedEvent.Event.Metadata));
			var topics = ((JArray)eventMetadata["topics"]).ToObject<object[]>();
			var recordedEventHandlingTypes = subscriberType
				.GetMessageHandlerTypes()
				.Select(x => x.GetGenericArguments()[0]);
			var recordedEventTypes = topics.Join(recordedEventHandlingTypes, x => x, x => x.GetGenericArguments()[0].GetEventStoreName(), (x, y) => y);
			var recordedEventType = recordedEventTypes.First();
			var recordedEvent = new
			{
				resolvedEvent.Event.EventNumber,
				resolvedEvent.Event.EventId,
				resolvedEvent.Event.Created,
				Event = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(resolvedEvent.Event.Data))
			};
			return Impromptu.CoerceConvert(recordedEvent, recordedEventType);
		}

		
	}
}
