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
	public sealed class SubscriberRegistry : IEnumerable<Func<Func<IEventStoreConnection>, Task<Subscriber>>>
	{
		private readonly IEnumerable<Func<Func<IEventStoreConnection>, Task<Subscriber>>> _createSubscribers;

		private SubscriberRegistry(IEnumerable<Func<Func<IEventStoreConnection>, Task<Subscriber>>> createSubscribers)
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
				_createSubscribers.Concat(new Func<Func<IEventStoreConnection>, Task<Subscriber>>[]
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
				_createSubscribers.Concat(new Func<Func<IEventStoreConnection>, Task<Subscriber>>[]
				{
					createConnection =>
						Subscriber.StartVolatileSubscriber(
							createConnection,
							typeof(TSubscription).GetEventStoreName(),
							handleResolvedEvent)
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
				_createSubscribers.Concat(new Func<Func<IEventStoreConnection>, Task<Subscriber>>[]
				{
					createConnection =>
						Subscriber.StartPersistentSubscriber(
							createConnection,
							typeof(TSubscription).GetEventStoreName(),
							typeof(TSubscriber).GetEventStoreName(),
							handleResolvedEvent)
				}));
		}

		public static SubscriberRegistry Create()
		{
			return new SubscriberRegistry(Enumerable.Empty<Func<Func<IEventStoreConnection>, Task<Subscriber>>>());
		}

		public IEnumerator<Func<Func<IEventStoreConnection>, Task<Subscriber>>> GetEnumerator()
		{
			return _createSubscribers.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
