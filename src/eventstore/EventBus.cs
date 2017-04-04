using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

using EventStore.ClientAPI;

using ImpromptuInterface;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using shared;

namespace eventstore
{
	public sealed class EventBus
	{
		private readonly IEnumerable<Func<Task>> _subscriptions;
		private readonly Func<IEventStoreConnection> _createConnection;

		public EventBus(Func<IEventStoreConnection> createConnection)
			: this(createConnection, Enumerable.Empty<Func<Task>>())
		{

		}

		private EventBus(Func<IEventStoreConnection> createConnection, IEnumerable<Func<Task>> subscriptions)
		{
			_createConnection = createConnection;
			_subscriptions = subscriptions;
		}

        public EventBus RegisterCatchupSubscriber<TSubscription>(TSubscription subscriber, Func<Task<long?>> getCheckpoint, 
			Func<Func<ResolvedEvent, Task<ResolvedEvent>>, Func<ResolvedEvent, Task<ResolvedEvent>>> configureEventHandling = null) where TSubscription : IMessageHandler
        {
			return new EventBus(_createConnection,
				_subscriptions.Concat(new Func<Task>[]
				{
					new CatchUpSubscription(
						_createConnection,
						typeof(TSubscription).GetEventStoreName(),
						(configureEventHandling ?? (x => x))(CreateEventHandle(subscriber)),
						TimeSpan.FromSeconds(1),
						getCheckpoint).Start
				}));
		}

		public EventBus RegisterVolatileSubscriber<TSubscription>(TSubscription subscriber, 
			Func<Func<ResolvedEvent, Task<ResolvedEvent>>, Func<ResolvedEvent, Task<ResolvedEvent>>> configureEventHandling = null) where TSubscription : IMessageHandler
		{
			return new EventBus(_createConnection,
				_subscriptions.Concat(new Func<Task>[]
				{
					new VolatileSubscription(
						_createConnection,
						typeof(TSubscription).GetEventStoreName(),
						(configureEventHandling ?? (x => x))(CreateEventHandle(subscriber)),
						TimeSpan.FromSeconds(1)).Start
				}));
		}

		public EventBus RegisterPersistentSubscriber<TSubscription>(TSubscription subscriber, 
			Func<Func<ResolvedEvent, Task<ResolvedEvent>>, Func<ResolvedEvent, Task<ResolvedEvent>>> configureEventHandling = null) where TSubscription : IMessageHandler
		{
			return new EventBus(_createConnection,
				_subscriptions.Concat(new Func<Task>[]
				{
					new PersistentSubscription(
						_createConnection,
						typeof(TSubscription).GetEventStoreName(),
						subscriber.GetType().GetEventStoreName(),
						(configureEventHandling ?? (x => x))(CreateEventHandle(subscriber)),
						TimeSpan.FromSeconds(1)).Start
				}));
		}

		public void Start()
		{
            Parallel.ForEach(_subscriptions, start => start());
		}

		private static Func<ResolvedEvent, Task<ResolvedEvent>> CreateEventHandle(IMessageHandler subscriber)
		{
			var eventHandlingTypes = subscriber
				.GetType()
				.GetMessageHandlerTypes()
				.Select(x => x.GetGenericArguments()[0].GetGenericArguments()[0])
				.ToArray();

			return async resolvedEvent =>
			{
				var recordedEvent = DeserializeEvent(eventHandlingTypes, resolvedEvent);
				await HandleEvent(subscriber, (dynamic)recordedEvent);
				return resolvedEvent;
			};
		}


		private static object DeserializeEvent(IEnumerable<Type> eventTypes, ResolvedEvent resolvedEvent)
		{
			var eventMetadata = JsonConvert.DeserializeObject<Dictionary<string, object>>(Encoding.UTF8.GetString(resolvedEvent.Event.Metadata));
			var topics = ((JArray)eventMetadata[EventHeaderKey.Topics]).ToObject<object[]>();
			var eventType = topics.Join(eventTypes, x => x, x => x.GetEventStoreName(), (x, y) => y).First();
			var recordedEvent = new
			{
				resolvedEvent.OriginalEventNumber,
				resolvedEvent.Event.EventStreamId,
				resolvedEvent.Event.EventNumber,
				resolvedEvent.Event.EventId,
				resolvedEvent.Event.Created,
				Event = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(resolvedEvent.Event.Data))
			};
			return Impromptu.CoerceConvert(recordedEvent, typeof(IRecordedEvent<>).MakeGenericType(eventType));
		}

		private static Task HandleEvent<TRecordedEvent>(IMessageHandler subscriber, TRecordedEvent recordedEvent)
		{
			var handler = (IMessageHandler<TRecordedEvent, Task>)subscriber;
			return handler.Handle(recordedEvent);
		}
	}
}
