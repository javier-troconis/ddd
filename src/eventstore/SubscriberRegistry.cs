using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using shared;

namespace eventstore
{
	public struct SubscriberRegistryEntry
	{
		public readonly string SubscriberName;
		public readonly Func<Func<IEventStoreConnection>, Task<Subscriber>> StartSubscriber;

		internal SubscriberRegistryEntry(string subscriberName, Func<Func<IEventStoreConnection>, Task<Subscriber>> startSubscriber)
		{
			SubscriberName = subscriberName;
			StartSubscriber = startSubscriber;
		}
	}

	public struct SubscriberRegistry : IEnumerable<SubscriberRegistryEntry>
	{
		private readonly IEnumerable<SubscriberRegistryEntry> _createSubscribers;

		private SubscriberRegistry(IEnumerable<SubscriberRegistryEntry> createSubscribers)
		{
			_createSubscribers = createSubscribers;
		}

		public SubscriberRegistry RegisterCatchupSubscriber<TSubscriber>(TSubscriber subscriber, Func<Task<long?>> getCheckpoint, Func<ResolvedEvent, string> getEventHandlingQueueKey = null) where TSubscriber : IMessageHandler
		{
			return RegisterCatchupSubscriber<TSubscriber, TSubscriber>(subscriber, getCheckpoint, getEventHandlingQueueKey);
		}

		public SubscriberRegistry RegisterCatchupSubscriber<TSubscription, TSubscriber>(TSubscriber subscriber, Func<Task<long?>> getCheckpoint, Func<ResolvedEvent, string> getEventHandlingQueueKey = null) where TSubscription : IMessageHandler where TSubscriber : TSubscription
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

		public SubscriberRegistry RegisterCatchupSubscriber<TSubscription>(Func<ResolvedEvent, Task> handleResolvedEvent, Func<Task<long?>> getCheckpoint, Func<ResolvedEvent, string> getEventHandlingQueueKey = null) where TSubscription : IMessageHandler
		{
			return new SubscriberRegistry(
				_createSubscribers.Concat(new[]
				{
					new SubscriberRegistryEntry
					(
						typeof(TSubscription).GetEventStoreName(),
						createConnection =>
							Subscriber.StartCatchUpSubscriber(
								createConnection,
								typeof(TSubscription).GetEventStoreName(),
								handleResolvedEvent,
								getCheckpoint,
								getEventHandlingQueueKey ?? (resolvedEvent => string.Empty))
					)
				}));
		}

		public SubscriberRegistry RegisterVolatileSubscriber<TSubscriber>(TSubscriber subscriber) where TSubscriber : IMessageHandler
		{
			return RegisterVolatileSubscriber<TSubscriber, TSubscriber>(subscriber);
		}

		public SubscriberRegistry RegisterVolatileSubscriber<TSubscription, TSubscriber>(TSubscriber subscriber) where TSubscription : IMessageHandler where TSubscriber : TSubscription
		{
			return RegisterVolatileSubscriber<TSubscription>
			(
				SubscriberResolvedEventHandleFactory
					.CreateSubscriberResolvedEventHandle<TSubscriber, Task>(delegate { return Task.CompletedTask; })
					.Partial(subscriber)
			);
		}

		public SubscriberRegistry RegisterVolatileSubscriber<TSubscription>(Func<ResolvedEvent, Task> handleResolvedEvent) where TSubscription : IMessageHandler
		{
			return new SubscriberRegistry(
				_createSubscribers.Concat(new []
				{
					new SubscriberRegistryEntry
					(
						typeof(TSubscription).GetEventStoreName(),
						createConnection =>
							Subscriber.StartVolatileSubscriber(
								createConnection,
								typeof(TSubscription).GetEventStoreName(),
								handleResolvedEvent)
					)
				}));
		}

		public SubscriberRegistry RegisterPersistentSubscriber<TSubscriber>(TSubscriber subscriber) where TSubscriber : IMessageHandler
		{
			return RegisterPersistentSubscriber<TSubscriber, TSubscriber>(subscriber);
		}

		public SubscriberRegistry RegisterPersistentSubscriber<TSubscription, TSubscriber>(TSubscriber subscriber) where TSubscription : IMessageHandler where TSubscriber : TSubscription
		{
			return RegisterPersistentSubscriber<TSubscription, TSubscriber>
			(
				SubscriberResolvedEventHandleFactory
					.CreateSubscriberResolvedEventHandle<TSubscriber, Task>(delegate { return Task.CompletedTask; })
					.Partial(subscriber)
			);
		}

		public SubscriberRegistry RegisterPersistentSubscriber<TSubscription, TSubscriber>(Func<ResolvedEvent, Task> handleResolvedEvent) where TSubscription : IMessageHandler
		{
			return new SubscriberRegistry(
				_createSubscribers.Concat(new []
				{
					new SubscriberRegistryEntry
					( 
						typeof(TSubscriber).GetEventStoreName(),
						createConnection =>
							Subscriber.StartPersistentSubscriber(
								createConnection,
								typeof(TSubscription).GetEventStoreName(),
								typeof(TSubscriber).GetEventStoreName(),
								handleResolvedEvent)
					)
				}));
		}

		public static SubscriberRegistry CreateSubscriberRegistry()
		{
			return new SubscriberRegistry(Enumerable.Empty<SubscriberRegistryEntry>());
		}

		public IEnumerator<SubscriberRegistryEntry> GetEnumerator()
		{
			return _createSubscribers.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
