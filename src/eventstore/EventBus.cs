using System;
using System.Collections.Generic;
using System.Linq;
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

		public EventBus RegisterCatchupSubscriber<TSubscriber>(TSubscriber subscriber, Func<Task<long?>> getCheckpoint, 
			Func<Func<ResolvedEvent, Task<ResolvedEvent>>, Func<ResolvedEvent, Task<ResolvedEvent>>> configureSubscriberHandle = null)
		{
            var handle = HandleResolvedEvent.Partial(subscriber).PipeForward(configureSubscriberHandle ?? (x => x));
            var streamName = typeof(TSubscriber).GetEventStoreName();
			return new EventBus(_createConnection,
				_subscriptions.Concat(new Func<Task>[]
				{
					new CatchUpSubscription(
						_createConnection,
						streamName,
						handle,
						TimeSpan.FromSeconds(1),
						getCheckpoint).Start
				}));
		}

		public EventBus RegisterVolatileSubscriber<TSubscriber>(TSubscriber subscriber, 
			Func<Func<ResolvedEvent, Task<ResolvedEvent>>, Func<ResolvedEvent, Task<ResolvedEvent>>> configureSubscriberHandle = null)
		{
            var handle = HandleResolvedEvent.Partial(subscriber).PipeForward(configureSubscriberHandle ?? (x => x));
            var streamName = typeof(TSubscriber).GetEventStoreName();
			return new EventBus(_createConnection,
				_subscriptions.Concat(new Func<Task>[]
				{
					new VolatileSubscription(
						_createConnection,
						streamName,
						handle,
						TimeSpan.FromSeconds(1)).Start
				}));
		}

		public EventBus RegisterPersistentSubscriber<TSubscriber>(TSubscriber subscriber, 
			Func<Func<ResolvedEvent, Task<ResolvedEvent>>, Func<ResolvedEvent, Task<ResolvedEvent>>> configureSubscriberHandle = null)
		{
            var handle = HandleResolvedEvent.Partial(subscriber).PipeForward(configureSubscriberHandle ?? (x => x));
            var streamName = typeof(TSubscriber).GetEventStoreName();
			var groupName = subscriber.GetType().GetEventStoreName();
			return new EventBus(_createConnection,
				_subscriptions.Concat(new Func<Task>[]
				{
					new PersistentSubscription(
						_createConnection,
						streamName,
						groupName,
						handle,
						TimeSpan.FromSeconds(1)).Start
				}));
		}

		public void Start()
		{
            Parallel.ForEach(_subscriptions, start => start());
		}

        private static readonly Func<object, ResolvedEvent, Task<ResolvedEvent>> HandleResolvedEvent = 
            async (subscriber, resolvedEvent) =>
        {
            var eventHandlingTypes = subscriber
                .GetType()
                .GetMessageHandlerTypes()
                .Select(x => x.GetGenericArguments()[0].GetGenericArguments()[0]);
            var recordedEvent = RecordedEventDeserializer.DeserializeRecordedEvent(eventHandlingTypes, resolvedEvent);
            await HandleRecordedEvent(subscriber, (dynamic)recordedEvent);
            return resolvedEvent;
        };

        private static Task HandleRecordedEvent<TRecordedEvent>(object subscriber, TRecordedEvent recordedEvent)
		{
			var handler = (IMessageHandler<TRecordedEvent, Task>)subscriber;
			return handler.Handle(recordedEvent);
		}
	}
}
