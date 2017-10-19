using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using shared;
using System.Collections.ObjectModel;

namespace eventstore
{
    public delegate Task<Subscriber> StartSubscriber(Func<IEventStoreConnection> createConnection);

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

    public struct SubscriberRegistry
    {
        private readonly IDictionary<string, StartSubscriber> _subscriberRegistrations;

        private SubscriberRegistry(IDictionary<string, StartSubscriber> subscriberRegistrations)
        {
            _subscriberRegistrations = subscriberRegistrations;
        }

        public SubscriberRegistry RegisterCatchupSubscriber<TSubscriber>(TSubscriber subscriber, Func<Task<long?>> getCheckpoint, Func<CatchupSubscriberRegistrationOptions, CatchupSubscriberRegistrationOptions> configureRegistration = null) where TSubscriber : IMessageHandler
        {
            var handleEvent = subscriber.CreateSubscriberEventHandle();
            return RegisterCatchupSubscriber<TSubscriber>
            (
                handleEvent,
                getCheckpoint,
                configureRegistration
            );
        }

        public SubscriberRegistry RegisterCatchupSubscriber<TSubscriber>(Func<ResolvedEvent, Task> handleEvent, Func<Task<long?>> getCheckpoint, Func<CatchupSubscriberRegistrationOptions, CatchupSubscriberRegistrationOptions> configureRegistration = null) where TSubscriber : IMessageHandler
        {
            var registrationConfiguration =
                (configureRegistration ?? (x => x))(
                    new CatchupSubscriberRegistrationOptions(typeof(TSubscriber).GetEventStoreName(), resolvedEvent => "default"));
            return new SubscriberRegistry
                (
                    new Dictionary<string, StartSubscriber>
                        {
                            {
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
                            }
                        }
                    .Merge(_subscriberRegistrations)
                );
        }

        public SubscriberRegistry RegisterVolatileSubscriber<TSubscriber>(TSubscriber subscriber, Func<VolatileSubscriberRegistrationOptions, VolatileSubscriberRegistrationOptions> configureRegistration = null) where TSubscriber : IMessageHandler
        {
            var handleEvent = subscriber.CreateSubscriberEventHandle();
            return RegisterVolatileSubscriber<TSubscriber>
            (
                handleEvent,
                configureRegistration
            );
        }

        public SubscriberRegistry RegisterVolatileSubscriber<TSubscriber>(Func<ResolvedEvent, Task> handleEvent, Func<VolatileSubscriberRegistrationOptions, VolatileSubscriberRegistrationOptions> configureRegistration = null) where TSubscriber : IMessageHandler
        {
            var registrationConfiguration =
                (configureRegistration ?? (x => x))(
                    new VolatileSubscriberRegistrationOptions(typeof(TSubscriber).GetEventStoreName()));
            return new SubscriberRegistry
                (
                     new Dictionary<string, StartSubscriber>
                        {
                            {
                                typeof(TSubscriber).GetEventStoreName(),
                                createConnection =>
                                    Subscriber.StartVolatileSubscriber
                                    (
                                        createConnection,
                                        registrationConfiguration.SubscriptionStream,
                                        handleEvent
                                    )
                            }
                        }
                    .Merge(_subscriberRegistrations)
                );
        }


        public SubscriberRegistry RegisterPersistentSubscriber<TSubscriber>(TSubscriber subscriber, Func<PersistentSubscriberRegistrationOptions, PersistentSubscriberRegistrationOptions> configureRegistration = null) where TSubscriber : IMessageHandler
        {
            var handleEvent = subscriber.CreateSubscriberEventHandle();
            return RegisterPersistentSubscriber<TSubscriber>
                (
                    handleEvent,
                    configureRegistration
                );
        }

        public SubscriberRegistry RegisterPersistentSubscriber<TSubscriber>(Func<ResolvedEvent, Task> handleEvent, Func<PersistentSubscriberRegistrationOptions, PersistentSubscriberRegistrationOptions> configureRegistration = null) where TSubscriber : IMessageHandler
        {
            var registrationConfiguration =
                (configureRegistration ?? (x => x))(
                    new PersistentSubscriberRegistrationOptions(typeof(TSubscriber).GetEventStoreName()));
            return new SubscriberRegistry
                (
                     new Dictionary<string, StartSubscriber>
                        {
                            {
                                typeof(TSubscriber).GetEventStoreName(),
                                createConnection =>
                                    Subscriber.StartPersistentSubscriber
                                    (
                                        createConnection,
                                        registrationConfiguration.SubscriptionStream,
                                        typeof(TSubscriber).GetEventStoreName(),
                                        handleEvent
                                    )
                            }
                        }
                    .Merge(_subscriberRegistrations)
                );
        }

        public static implicit operator ReadOnlyDictionary<string, StartSubscriber> (SubscriberRegistry subscriberRegistry)
        {
            return new ReadOnlyDictionary<string, StartSubscriber>(subscriberRegistry._subscriberRegistrations);
        }

        public static SubscriberRegistry CreateSubscriberRegistry()
        {
            return new SubscriberRegistry(new Dictionary<string, StartSubscriber>());
        }
    }
}
