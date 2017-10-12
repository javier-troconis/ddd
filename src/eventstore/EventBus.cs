using System;
using System.Collections;
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
	public sealed class EventBusRegistry : IEnumerable<Func<Func<IEventStoreConnection>, Task<Subscriber>>>
	{
		private readonly IEnumerable<Func<Func<IEventStoreConnection>, Task<Subscriber>>> _subscriberStarters;

		private EventBusRegistry(IEnumerable<Func<Func<IEventStoreConnection>, Task<Subscriber>>> subscriberStarters)
		{
			_subscriberStarters = subscriberStarters;
		}

		public EventBusRegistry RegisterCatchupSubscriber<TSubscriber>(TSubscriber subscriber, Func<Task<long?>> getCheckpoint, Func<ResolvedEvent, string> getEventHandlingQueueKey = null) where TSubscriber : IMessageHandler
		{
			return RegisterCatchupSubscriber<TSubscriber, TSubscriber>(subscriber, getCheckpoint, getEventHandlingQueueKey);
		}

		public EventBusRegistry RegisterCatchupSubscriber<TSubscription, TSubscriber>(TSubscriber subscriber, Func<Task<long?>> getCheckpoint, Func<ResolvedEvent, string> getEventHandlingQueueKey = null) where TSubscription : IMessageHandler where TSubscriber : TSubscription
		{
			return RegisterCatchupSubscriber<TSubscription>
			(
				SubscriberResolvedEventHandleFactory
					.CreateSubscriberResolvedEventHandle<TSubscriber, Task>(delegate { return Task.CompletedTask; })
					.Partial(subscriber),
				getCheckpoint,
				getEventHandlingQueueKey
			);
		}

		public EventBusRegistry RegisterCatchupSubscriber<TSubscription>(Func<ResolvedEvent, Task> handleResolvedEvent, Func<Task<long?>> getCheckpoint, Func<ResolvedEvent, string> getEventHandlingQueueKey = null) where TSubscription : IMessageHandler
		{
			return new EventBusRegistry(
				_subscriberStarters.Concat(new Func<Func<IEventStoreConnection>, Task<Subscriber>>[]
				{
					createConnection =>
						Subscriber.StartCatchUpSubscriber(
							createConnection,
							typeof(TSubscription).GetEventStoreName(),
							handleResolvedEvent,
							getCheckpoint,
							getEventHandlingQueueKey ?? (resolvedEvent => string.Empty))

				}));
		}

		public EventBusRegistry RegisterVolatileSubscriber<TSubscriber>(TSubscriber subscriber) where TSubscriber : IMessageHandler
		{
			return RegisterVolatileSubscriber<TSubscriber, TSubscriber>(subscriber);
		}

		public EventBusRegistry RegisterVolatileSubscriber<TSubscription, TSubscriber>(TSubscriber subscriber) where TSubscription : IMessageHandler where TSubscriber : TSubscription
		{
			return RegisterVolatileSubscriber<TSubscription>
			(
				SubscriberResolvedEventHandleFactory
					.CreateSubscriberResolvedEventHandle<TSubscriber, Task>(delegate { return Task.CompletedTask; })
					.Partial(subscriber)
			);
		}

		public EventBusRegistry RegisterVolatileSubscriber<TSubscription>(Func<ResolvedEvent, Task> handleResolvedEvent) where TSubscription : IMessageHandler
		{
			return new EventBusRegistry(
				_subscriberStarters.Concat(new Func<Func<IEventStoreConnection>, Task<Subscriber>>[]
				{
					createConnection =>
						Subscriber.StartVolatileSubscriber(
							createConnection,
							typeof(TSubscription).GetEventStoreName(),
							handleResolvedEvent)
				}));
		}

		public EventBusRegistry RegisterPersistentSubscriber<TSubscriber>(TSubscriber subscriber) where TSubscriber : IMessageHandler
		{
			return RegisterPersistentSubscriber<TSubscriber, TSubscriber>(subscriber);
		}

		public EventBusRegistry RegisterPersistentSubscriber<TSubscription, TSubscriber>(TSubscriber subscriber) where TSubscription : IMessageHandler where TSubscriber : TSubscription
		{
			return RegisterPersistentSubscriber<TSubscription, TSubscriber>
			(
				SubscriberResolvedEventHandleFactory
					.CreateSubscriberResolvedEventHandle<TSubscriber, Task>(delegate { return Task.CompletedTask; })
					.Partial(subscriber)
			);
		}

		public EventBusRegistry RegisterPersistentSubscriber<TSubscription, TSubscriber>(Func<ResolvedEvent, Task> handleResolvedEvent) where TSubscription : IMessageHandler
		{
			return new EventBusRegistry(
				_subscriberStarters.Concat(new Func<Func<IEventStoreConnection>, Task<Subscriber>>[]
				{
					createConnection =>
						Subscriber.StartPersistentSubscriber(
							createConnection,
							typeof(TSubscription).GetEventStoreName(),
							typeof(TSubscriber).GetEventStoreName(),
							handleResolvedEvent)
				}));
		}

		public static EventBusRegistry Create()
		{
			return new EventBusRegistry(Enumerable.Empty<Func<Func<IEventStoreConnection>, Task<Subscriber>>>());
		}

		public IEnumerator<Func<Func<IEventStoreConnection>, Task<Subscriber>>> GetEnumerator()
		{
			return _subscriberStarters.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

	public sealed class EventBus
	{
		private readonly IEnumerable<Subscriber> _subscribers;

		public EventBus(IEnumerable<Subscriber> subscribers)
		{
			_subscribers = subscribers;
		}

		public void Stop()
		{
			Parallel.ForEach(_subscribers, subscriber => subscriber.Stop());
		}

		public static async Task<EventBus> Start(Func<IEventStoreConnection> createConnection, Func<EventBusRegistry, EventBusRegistry> registerSubscribers)
		{
			var subscriberStarters = registerSubscribers(EventBusRegistry.Create());
			var subscribers = await Task.WhenAll(subscriberStarters.Select(startSubscriber => startSubscriber(createConnection)));
			return new EventBus(subscribers);
		}
	}
}
