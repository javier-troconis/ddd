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

	public struct CatchupSubscriberRegistrationOptions
	{
		public readonly string SubscriptionStream;
		public readonly Func<ResolvedEvent, string> GetEventHandlingQueueKey;

		internal CatchupSubscriberRegistrationOptions(string subscriptionStream, Func<ResolvedEvent, string> getEventHandlingQueueKey)
		{
			SubscriptionStream = subscriptionStream;
			GetEventHandlingQueueKey = getEventHandlingQueueKey;
		}

		public CatchupSubscriberRegistrationOptions SetEventHandlingQueueKey(Func<ResolvedEvent, string> getEventHandlingQueueKey)
		{
			return new CatchupSubscriberRegistrationOptions(SubscriptionStream, getEventHandlingQueueKey);
		}

		public CatchupSubscriberRegistrationOptions SetSubscriptionStream<TSubscriptionStream>() where TSubscriptionStream : IMessageHandler
		{
			return new CatchupSubscriberRegistrationOptions(typeof(TSubscriptionStream).GetEventStoreName(), GetEventHandlingQueueKey);
		}
	}

	public struct VolatileSubscriberRegistrationOptions
	{
		public readonly string SubscriptionStream;

		internal VolatileSubscriberRegistrationOptions(string subscriptionStream)
		{
			SubscriptionStream = subscriptionStream;
		}

		public VolatileSubscriberRegistrationOptions SetSubscriptionStream<TSubscriptionStream>() where TSubscriptionStream : IMessageHandler
		{
			return new VolatileSubscriberRegistrationOptions(typeof(TSubscriptionStream).GetEventStoreName());
		}
	}

	public struct PersistentSubscriberRegistrationOptions
	{
		public readonly string SubscriptionStream;

		internal PersistentSubscriberRegistrationOptions(string subscriptionStream)
		{
			SubscriptionStream = subscriptionStream;
		}

		public PersistentSubscriberRegistrationOptions SetSubscriptionStream<TSubscriptionStream>() where TSubscriptionStream : IMessageHandler
		{
			return new PersistentSubscriberRegistrationOptions(typeof(TSubscriptionStream).GetEventStoreName());
		}
	}

	public struct SubscriberRegistry : IEnumerable<SubscriberRegistration>
	{
		private readonly IEnumerable<SubscriberRegistration> _subscriberRegistrations;

		private SubscriberRegistry(IEnumerable<SubscriberRegistration> subscriberRegistrations)
		{
			_subscriberRegistrations = subscriberRegistrations;
		}

		public SubscriberRegistry RegisterCatchupSubscriber<TSubscriber>(TSubscriber subscriber, Func<Task<long?>> getCheckpoint, Func<CatchupSubscriberRegistrationOptions, CatchupSubscriberRegistrationOptions> configureRegistration = null) where TSubscriber : IMessageHandler
		{
			return RegisterCatchupSubscriber<TSubscriber>
			(
				subscriber.CreateSubscriberResolvedEventHandle(),
				getCheckpoint,
				configureRegistration
			);
		}

		public SubscriberRegistry RegisterCatchupSubscriber<TSubscriber>(Func<ResolvedEvent, Task> handleEvent, Func<Task<long?>> getCheckpoint, Func<CatchupSubscriberRegistrationOptions, CatchupSubscriberRegistrationOptions> configureRegistration = null) where TSubscriber : IMessageHandler
		{
			var registrationConfiguration =
				(configureRegistration ?? (x => x))(
					new CatchupSubscriberRegistrationOptions(typeof(TSubscriber).GetEventStoreName(), resolvedEvent => string.Empty));
			return new SubscriberRegistry(
				_subscriberRegistrations.Concat(new[]
				{
					new SubscriberRegistration
					(
						typeof(TSubscriber).GetEventStoreName(),
						createConnection =>
							Subscriber.StartCatchUpSubscriber
							(
								createConnection,
								registrationConfiguration.SubscriptionStream,
								handleEvent,
								getCheckpoint,
								registrationConfiguration.GetEventHandlingQueueKey
							)
					)
				}));
		}

		public SubscriberRegistry RegisterVolatileSubscriber<TSubscriber>(TSubscriber subscriber, Func<VolatileSubscriberRegistrationOptions, VolatileSubscriberRegistrationOptions> configureRegistration = null) where TSubscriber : IMessageHandler
		{
			return RegisterVolatileSubscriber<TSubscriber>
			(
				subscriber.CreateSubscriberResolvedEventHandle(),
				configureRegistration
			);
		}

		public SubscriberRegistry RegisterVolatileSubscriber<TSubscriber>(Func<ResolvedEvent, Task> handleEvent, Func<VolatileSubscriberRegistrationOptions, VolatileSubscriberRegistrationOptions> configureRegistration = null) where TSubscriber : IMessageHandler
		{
			var registrationConfiguration =
				(configureRegistration ?? (x => x))(
					new VolatileSubscriberRegistrationOptions(typeof(TSubscriber).GetEventStoreName()));
			return new SubscriberRegistry(
				_subscriberRegistrations.Concat(new []
				{
					new SubscriberRegistration
					(
						typeof(TSubscriber).GetEventStoreName(),
						createConnection =>
							Subscriber.StartVolatileSubscriber(
								createConnection,
								registrationConfiguration.SubscriptionStream,
								handleEvent)
					)
				}));
		}


		public SubscriberRegistry RegisterPersistentSubscriber<TSubscriber>(TSubscriber subscriber, Func<PersistentSubscriberRegistrationOptions, PersistentSubscriberRegistrationOptions> configureRegistration = null) where TSubscriber : IMessageHandler
		{
			return RegisterPersistentSubscriber<TSubscriber>
				(
					subscriber.CreateSubscriberResolvedEventHandle(), 
					configureRegistration
				);
		}

		public SubscriberRegistry RegisterPersistentSubscriber<TSubscriber>(Func<ResolvedEvent, Task> handleEvent, Func<PersistentSubscriberRegistrationOptions, PersistentSubscriberRegistrationOptions> configureRegistration = null) where TSubscriber : IMessageHandler
		{
			var registrationConfiguration =
				(configureRegistration ?? (x => x))(
					new PersistentSubscriberRegistrationOptions(typeof(TSubscriber).GetEventStoreName()));
			return new SubscriberRegistry(
				_subscriberRegistrations.Concat(new []
				{
					new SubscriberRegistration
					( 
						typeof(TSubscriber).GetEventStoreName(),
						createConnection =>
							Subscriber.StartPersistentSubscriber(
								createConnection,
								registrationConfiguration.SubscriptionStream,
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
