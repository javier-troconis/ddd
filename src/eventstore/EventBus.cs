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
			Func<Func<ResolvedEvent, Task<ResolvedEvent>>, Func<ResolvedEvent, Task<ResolvedEvent>>> processEventHandling = null) where TSubscription : IMessageHandler
		{
			return RegisterCatchupSubscriber<TSubscription, TSubscription>(subscriber, getCheckpoint, processEventHandling);
		}

		public EventBus RegisterCatchupSubscriber<TSubscription, TSubscriber>(TSubscriber subscriber, Func<Task<long?>> getCheckpoint,
			Func<Func<ResolvedEvent, Task<ResolvedEvent>>, Func<ResolvedEvent, Task<ResolvedEvent>>> processEventHandling = null) where TSubscriber : TSubscription where TSubscription : IMessageHandler
		{
			var subscriberResolvedEventHandler = CreateSubscriberResolvedEventHandler(subscriber);
			processEventHandling = processEventHandling ?? (x => x);

			return new EventBus(_createConnection,
				_subscriptions.Concat(new Func<Task>[]
				{
					new CatchUpSubscriber(
						_createConnection,
						typeof(TSubscription).GetEventStoreName(),
						(subscription, resolvedEvent) => 
							processEventHandling(
								CreateSubscriptionResolvedEventHandler(
									subscription, 
									subscriberResolvedEventHandler))
									(resolvedEvent),
						TimeSpan.FromSeconds(1),
						getCheckpoint)
						.Start
				}));
		}

		public EventBus RegisterVolatileSubscriber<TSubscription>(TSubscription subscriber,
			Func<Func<ResolvedEvent, Task<ResolvedEvent>>, Func<ResolvedEvent, Task<ResolvedEvent>>> processEventHandling = null) where TSubscription : IMessageHandler
		{
			return RegisterVolatileSubscriber<TSubscription, TSubscription>(subscriber, processEventHandling);
		}

		public EventBus RegisterVolatileSubscriber<TSubscription, TSubscriber>(TSubscriber subscriber,
			Func<Func<ResolvedEvent, Task<ResolvedEvent>>, Func<ResolvedEvent, Task<ResolvedEvent>>> processEventHandling = null) where TSubscriber : TSubscription where TSubscription : IMessageHandler
		{
			var subscriberResolvedEventHandler = CreateSubscriberResolvedEventHandler(subscriber);
			processEventHandling = processEventHandling ?? (x => x);

			return new EventBus(_createConnection,
				_subscriptions.Concat(new Func<Task>[]
				{
					new VolatileSubscriber(
						_createConnection,
						typeof(TSubscription).GetEventStoreName(),
						(subscription, resolvedEvent) =>
							processEventHandling(
									CreateSubscriptionResolvedEventHandler(
										subscription,
										subscriberResolvedEventHandler))
								(resolvedEvent),
						TimeSpan.FromSeconds(1))
						.Start
				}));
		}

		public EventBus RegisterPersistentSubscriber<TSubscription>(TSubscription subscriber,
			Func<Func<ResolvedEvent, Task<ResolvedEvent>>, Func<ResolvedEvent, Task<ResolvedEvent>>> processEventHandling = null) where TSubscription : IMessageHandler
		{
			return RegisterPersistentSubscriber<TSubscription, TSubscription>(subscriber, processEventHandling);
		}

		public EventBus RegisterPersistentSubscriber<TSubscription, TSubscriber>(TSubscriber subscriber,
			Func<Func<ResolvedEvent, Task<ResolvedEvent>>, Func<ResolvedEvent, Task<ResolvedEvent>>> processEventHandling = null) where TSubscriber : TSubscription where TSubscription : IMessageHandler
		{
			var subscriberResolvedEventHandler = CreateSubscriberResolvedEventHandler(subscriber);
			processEventHandling = processEventHandling ?? (x => x);

			void EventHandlingSucceeded(EventStorePersistentSubscriptionBase subscription, ResolvedEvent resolvedEvent)
			{
				subscription.Acknowledge(resolvedEvent);
			}

			void EventHandlingFailed(EventStorePersistentSubscriptionBase subscription, ResolvedEvent resolvedEvent, Exception exception)
			{
				subscription.Fail(resolvedEvent, PersistentSubscriptionNakEventAction.Unknown, exception.Message);
			}

			return new EventBus(_createConnection,
				_subscriptions.Concat(new Func<Task>[]
				{
					new PersistentSubscriber(
						_createConnection,
						typeof(TSubscription).GetEventStoreName(),
						typeof(TSubscriber).GetEventStoreName(),
						(subscription, resolvedEvent) =>
							processEventHandling(
									CreateSubscriptionResolvedEventHandler(
										subscription,
										subscriberResolvedEventHandler, 
										EventHandlingSucceeded,
										EventHandlingFailed))
								(resolvedEvent),
						TimeSpan.FromSeconds(1))
						.Start
				}));
		}

		public void Start()
		{
			Parallel.ForEach(_subscriptions, start => start());
		}

		private static Func<ResolvedEvent, Task<ResolvedEvent>> CreateSubscriptionResolvedEventHandler<TSubscription>(
			TSubscription subscription,
			Func<ResolvedEvent, Task<ResolvedEvent>> handleEvent,
			Action<TSubscription, ResolvedEvent> eventHandlingSucceeded = null,
			Action<TSubscription, ResolvedEvent, Exception> eventHandlingFailed = null)
		{
			eventHandlingSucceeded = eventHandlingSucceeded ?? delegate { };
			eventHandlingFailed = eventHandlingFailed ?? delegate { };
			return async resolvedEvent =>
			{
				try
				{
					await handleEvent(resolvedEvent);
				}
				catch (Exception ex)
				{
					eventHandlingFailed(subscription, resolvedEvent, ex);
					throw;
				}
				eventHandlingSucceeded(subscription, resolvedEvent);
				return resolvedEvent;
			};
		}

		private static Func<ResolvedEvent, Task<ResolvedEvent>> CreateSubscriberResolvedEventHandler(IMessageHandler subscriber)
		{
			//Task HandleEvent<TRecordedEvent>(TRecordedEvent recordedEvent)
			//{
			//	var handler = (IMessageHandler<TRecordedEvent, Task>)subscriber;
			//	return handler.Handle(recordedEvent);
			//}

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
			var eventType = topics.Join(eventTypes, x => x, x => x.GetEventStoreName(), (x, y) => y).FirstOrDefault();
			if (eventType == null)
			{
				throw new Exception($"Failed to deserialized event for topic {topics[0]}. The messagehandler for topic {topics[0]} has been removed from the subscriber but the subscription stream has not been updated.");
			}
			var recordedEvent = new
			{
				resolvedEvent.OriginalStreamId,
				resolvedEvent.OriginalEventNumber,
				resolvedEvent.Event.EventStreamId,
				resolvedEvent.Event.EventNumber,
				resolvedEvent.Event.EventId,
				resolvedEvent.Event.Created,
				Data = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(resolvedEvent.Event.Data))
			};
			var recordedEventType = typeof(IRecordedEvent<>).MakeGenericType(eventType);
			return Impromptu.CoerceConvert(recordedEvent, recordedEventType);
		}

		private static Task HandleEvent<TRecordedEvent>(IMessageHandler subscriber, TRecordedEvent recordedEvent)
		{
			var handler = (IMessageHandler<TRecordedEvent, Task>)subscriber;
			return handler.Handle(recordedEvent);
		}
	}
}
