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

		public SubscriberRegistry RegisterCatchupSubscriber<TSubscription>(TSubscription subscriber, Func<Task<long?>> getCheckpoint, Func<ResolvedEvent, string> getEventHandlingQueueKey = null) where TSubscription : IMessageHandler
		{
			var handleEvent =
				SubscriberResolvedEventHandleFactory
					.CreateSubscriberResolvedEventHandle<TSubscription, Task>(delegate { return Task.CompletedTask; })
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

		public SubscriberRegistry RegisterVolatileSubscriber<TSubscription>(TSubscription subscriber) where TSubscription : IMessageHandler
		{
			var handleEvent =
				SubscriberResolvedEventHandleFactory
					.CreateSubscriberResolvedEventHandle<TSubscription, Task>(delegate { return Task.CompletedTask; })
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


		public SubscriberRegistry RegisterPersistentSubscriber<TSubscription>(TSubscription subscriber) where TSubscription : IMessageHandler
		{
			return RegisterPersistentSubscriber<TSubscription, TSubscription>(subscriber);
		}

		public SubscriberRegistry RegisterPersistentSubscriber<TSubscription, TSubscriptionGroup>(TSubscriptionGroup subscriber) where TSubscription : IMessageHandler where TSubscriptionGroup : TSubscription
		{
			var handleEvent =
				SubscriberResolvedEventHandleFactory
					.CreateSubscriberResolvedEventHandle<TSubscriptionGroup, Task>(delegate { return Task.CompletedTask; })
					.Partial(subscriber);
			return RegisterPersistentSubscriber<TSubscription, TSubscriptionGroup>
			(
				handleEvent
			);
		}

		public SubscriberRegistry RegisterPersistentSubscriber<TSubscription>(Func<ResolvedEvent, Task> handleEvent) where TSubscription : IMessageHandler
		{
			return RegisterPersistentSubscriber<TSubscription, TSubscription>(handleEvent);
		}

		public SubscriberRegistry RegisterPersistentSubscriber<TSubscription, TSubscriptionGroup>(Func<ResolvedEvent, Task> handleEvent) where TSubscription : IMessageHandler where TSubscriptionGroup : TSubscription
		{
			return new SubscriberRegistry(
				_subscriberRegistrations.Concat(new []
				{
					new SubscriberRegistration
					( 
						typeof(TSubscriptionGroup).GetEventStoreName(),
						createConnection =>
							Subscriber.StartPersistentSubscriber(
								createConnection,
								typeof(TSubscription).GetEventStoreName(),
								typeof(TSubscriptionGroup).GetEventStoreName(),
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
