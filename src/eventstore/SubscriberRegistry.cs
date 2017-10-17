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
	public struct SubscriberRegistration
	{
		public readonly string SubscriberName;
		public readonly Func<Func<IEventStoreConnection>, Task<Subscriber>> StartSubscriber;

		internal SubscriberRegistration(string subscriberName, Func<Func<IEventStoreConnection>, Task<Subscriber>> startSubscriber)
		{
			SubscriberName = subscriberName;
			StartSubscriber = startSubscriber;
		}
	}

	public struct SubscriberRegistry : IEnumerable<SubscriberRegistration>
	{
		private readonly IEnumerable<SubscriberRegistration> _subscriberRegistrations;

		private SubscriberRegistry(IEnumerable<SubscriberRegistration> subscriberRegistrations)
		{
			_subscriberRegistrations = subscriberRegistrations;
		}

		public SubscriberRegistry RegisterCatchupSubscriber<TSubscriber>(TSubscriber subscriber, Func<Task<long?>> getCheckpoint, Func<ResolvedEvent, string> getEventHandlingQueueKey = null) where TSubscriber : IMessageHandler
		{
			return RegisterCatchupSubscriber<TSubscriber, TSubscriber>(subscriber, getCheckpoint, getEventHandlingQueueKey);
		}

		public SubscriberRegistry RegisterCatchupSubscriber<TSubscription, TSubscriber>(TSubscriber subscriber, Func<Task<long?>> getCheckpoint, Func<ResolvedEvent, string> getEventHandlingQueueKey = null) where TSubscription : IMessageHandler where TSubscriber : TSubscription
		{
			var handleEvent = 
				SubscriberResolvedEventHandleFactory
					.CreateSubscriberResolvedEventHandle<TSubscriber, Task>(delegate { return Task.CompletedTask; })
					.Partial(subscriber);
			return RegisterCatchupSubscriber<TSubscription>
			(
				handleEvent,
				getCheckpoint,
				getEventHandlingQueueKey
			);
		}

		public SubscriberRegistry RegisterCatchupSubscriber<TSubscription>(Func<ResolvedEvent, Task> handleEvent, Func<Task<long?>> getCheckpoint, Func<ResolvedEvent, string> getEventHandlingQueueKey = null) where TSubscription : IMessageHandler
		{
			return new SubscriberRegistry(
				_subscriberRegistrations.Concat(new[]
				{
					new SubscriberRegistration
					(
						typeof(TSubscription).GetEventStoreName(),
						createConnection =>
							Subscriber.StartCatchUpSubscriber(
								createConnection,
								typeof(TSubscription).GetEventStoreName(),
								handleEvent,
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
			var handleEvent = 
				SubscriberResolvedEventHandleFactory
					.CreateSubscriberResolvedEventHandle<TSubscriber, Task>(delegate { return Task.CompletedTask; })
					.Partial(subscriber);
			return RegisterVolatileSubscriber<TSubscription>
			(
				handleEvent
			);
		}

		public SubscriberRegistry RegisterVolatileSubscriber<TSubscription>(Func<ResolvedEvent, Task> handleEvent) where TSubscription : IMessageHandler
		{
			return new SubscriberRegistry(
				_subscriberRegistrations.Concat(new []
				{
					new SubscriberRegistration
					(
						typeof(TSubscription).GetEventStoreName(),
						createConnection =>
							Subscriber.StartVolatileSubscriber(
								createConnection,
								typeof(TSubscription).GetEventStoreName(),
								handleEvent)
					)
				}));
		}

		public SubscriberRegistry RegisterPersistentSubscriber<TSubscriber>(TSubscriber subscriber) where TSubscriber : IMessageHandler
		{
			return RegisterPersistentSubscriber<TSubscriber, TSubscriber>(subscriber);
		}

		public SubscriberRegistry RegisterPersistentSubscriber<TSubscription, TSubscriber>(TSubscriber subscriber) where TSubscription : IMessageHandler where TSubscriber : TSubscription
		{
			var handleEvent =
				SubscriberResolvedEventHandleFactory
					.CreateSubscriberResolvedEventHandle<TSubscriber, Task>(delegate { return Task.CompletedTask; })
					.Partial(subscriber);
			return RegisterPersistentSubscriber<TSubscription, TSubscriber>
			(
				handleEvent
			);
		}

		public SubscriberRegistry RegisterPersistentSubscriber<TSubscription, TSubscriber>(Func<ResolvedEvent, Task> handleEvent) where TSubscription : IMessageHandler
		{
			return new SubscriberRegistry(
				_subscriberRegistrations.Concat(new []
				{
					new SubscriberRegistration
					( 
						typeof(TSubscriber).GetEventStoreName(),
						createConnection =>
							Subscriber.StartPersistentSubscriber(
								createConnection,
								typeof(TSubscription).GetEventStoreName(),
								typeof(TSubscriber).GetEventStoreName(),
								handleEvent)
					)
				}));
		}

		public static SubscriberRegistry CreateSubscriberRegistry()
		{
			return new SubscriberRegistry(Enumerable.Empty<SubscriberRegistration>());
		}

		public IEnumerator<SubscriberRegistration> GetEnumerator()
		{
			return _subscriberRegistrations.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
