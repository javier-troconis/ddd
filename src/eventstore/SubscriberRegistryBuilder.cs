using EventStore.ClientAPI;
using shared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eventstore
{
    public interface ICatchupSubscriberRegistrationOptions : ISubscriberRegistrationOptions
    {
        string SubscriptionStreamName { get; }
        string SubscriberName { get; }
        Func<ResolvedEvent, string> GetEventHandlingQueueName { get; }
        Func<ResolvedEvent, Task> HandleEvent { get; }
    }

    public class CatchupSubscriberRegistrationOptions : ICatchupSubscriberRegistrationOptions
    {
        internal CatchupSubscriberRegistrationOptions(string subscriptionStreamName, string subscriberName, Func<ResolvedEvent, string> getEventHandlingQueueName, Func<ResolvedEvent, Task> handleEvent)
        {
            SubscriptionStreamName = subscriptionStreamName;
            SubscriberName = subscriberName;
            GetEventHandlingQueueName = getEventHandlingQueueName;
            HandleEvent = handleEvent;
        }

        public CatchupSubscriberRegistrationOptions SetEventHandlingQueueKey(Func<ResolvedEvent, string> getEventHandlingQueueKey)
        {
            return new CatchupSubscriberRegistrationOptions(SubscriptionStreamName, SubscriberName, getEventHandlingQueueKey, HandleEvent);
        }

        public CatchupSubscriberRegistrationOptions SetSubscriptionStreamName(EventStoreObjectName streamName)
        {
            return new CatchupSubscriberRegistrationOptions(streamName, SubscriberName, GetEventHandlingQueueName, HandleEvent);
        }

        public CatchupSubscriberRegistrationOptions SetSubscriberName(string subscriberName)
        {
            return new CatchupSubscriberRegistrationOptions(SubscriptionStreamName, subscriberName, GetEventHandlingQueueName, HandleEvent);
        }

        public CatchupSubscriberRegistrationOptions SetSubscriberNamingConvention(Func<string, string> subscriberNamingConvention)
        {
            return new CatchupSubscriberRegistrationOptions(SubscriptionStreamName, subscriberNamingConvention(SubscriberName), GetEventHandlingQueueName, HandleEvent);
        }

        public string SubscriptionStreamName { get; }
        public string SubscriberName { get; }
        public Func<ResolvedEvent, string> GetEventHandlingQueueName { get; }
        public Func<ResolvedEvent, Task> HandleEvent { get; }
    }

    public interface IVolatileSubscriberRegistrationOptions : ISubscriberRegistrationOptions
    {
        string SubscriptionStreamName { get; }
        string SubscriberName { get; }
    }

    public class VolatileSubscriberRegistrationOptions : IVolatileSubscriberRegistrationOptions
    {
        internal VolatileSubscriberRegistrationOptions(string subscriptionStreamName, string subscriberName)
        {
            SubscriptionStreamName = subscriptionStreamName;
            SubscriberName = subscriberName;
        }

        public VolatileSubscriberRegistrationOptions SetSubscriptionStreamName(EventStoreObjectName streamName)
        {
            return new VolatileSubscriberRegistrationOptions(streamName, SubscriberName);
        }

        public VolatileSubscriberRegistrationOptions SetSubscriberName(string subscriberName)
        {
            return new VolatileSubscriberRegistrationOptions(SubscriptionStreamName, subscriberName);
        }

        public VolatileSubscriberRegistrationOptions SetSubscriberNamingConvention(Func<string, string> subscriberNamingConvention)
        {
            return new VolatileSubscriberRegistrationOptions(SubscriptionStreamName, subscriberNamingConvention(SubscriberName));
        }

        public string SubscriptionStreamName { get; }
        public string SubscriberName { get; }
    }

    public interface IPersistentSubscriberRegistrationOptions : ISubscriberRegistrationOptions
    {
        string SubscriberName { get; }
        string SubscriptionStreamName { get; }
        string SubscriptionGroupName { get; }
    }

    public class PersistentSubscriberRegistrationOptions : IPersistentSubscriberRegistrationOptions
    {
        internal PersistentSubscriberRegistrationOptions(string subscriptionStreamName, string subscriptionGroupName, string subscriberName)
        {
            SubscriptionStreamName = subscriptionStreamName;
            SubscriptionGroupName = subscriptionGroupName;
            SubscriberName = subscriberName;
        }

        public PersistentSubscriberRegistrationOptions SetSubscriptionStreamName(EventStoreObjectName streamName)
        {
            return new PersistentSubscriberRegistrationOptions(streamName, SubscriptionGroupName, SubscriberName);
        }

        public PersistentSubscriberRegistrationOptions SetSubscriptionGroupName(EventStoreObjectName subscriptionGroupName)
        {
            return new PersistentSubscriberRegistrationOptions(SubscriptionStreamName, subscriptionGroupName, SubscriberName);
        }

        public PersistentSubscriberRegistrationOptions SetSubscriberName(string subscriberName)
        {
            return new PersistentSubscriberRegistrationOptions(SubscriptionStreamName, SubscriptionGroupName, subscriberName);
        }

        public PersistentSubscriberRegistrationOptions SetSubscriberNamingConvention(Func<string, string> subscriberNamingConvention)
        {
            return new PersistentSubscriberRegistrationOptions(SubscriptionStreamName, SubscriptionGroupName, subscriberNamingConvention(SubscriberName));
        }

        public string SubscriberName { get; }
        public string SubscriptionStreamName { get; }
        public string SubscriptionGroupName { get; }
    }

    public class SubscriberRegistryBuilder : ReadOnlyDictionary<string, ISubscriberRegistrationOptions>, ISubscriberRegistry
    {
        private SubscriberRegistryBuilder(IDictionary<string, ISubscriberRegistrationOptions> dictionary) : base(dictionary)
        {
          
        }

        
        public SubscriberRegistryBuilder RegisterCatchupSubscriber<TSubscriber>(TSubscriber subscriber, Func<Task<long?>> getCheckpoint, Func<CatchupSubscriberRegistrationOptions, ICatchupSubscriberRegistrationOptions> setRegistrationOptions = null) where TSubscriber : IMessageHandler
        {
            return RegisterCatchupSubscriber
            (
                typeof(TSubscriber).GetEventStoreObjectName(),
                typeof(TSubscriber).GetEventStoreObjectName(),
                subscriber.CreateSubscriberEventHandle(),
                getCheckpoint,
                setRegistrationOptions
            );
        }

        public SubscriberRegistryBuilder RegisterCatchupSubscriber(string subscriberName, EventStoreObjectName streamName, Func<ResolvedEvent, Task> handleEvent, Func<Task<long?>> getCheckpoint, Func<CatchupSubscriberRegistrationOptions, ICatchupSubscriberRegistrationOptions> setRegistrationOptions = null)
        {
            var registrationOptions = new CatchupSubscriberRegistrationOptions(subscriberName, streamName, resolvedEvent => string.Empty, handleEvent).PipeForward(setRegistrationOptions ?? (x => x));
            return new SubscriberRegistryBuilder
                (
                    Dictionary.Merge
                    (
                        new Dictionary<string, ISubscriberRegistrationOptions>
                            {
                                {
                                    subscriberName,
                                    registrationOptions
                                }
                            }
                    )
                );
        }
        
        /*

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
                    new VolatileSubscriberRegistrationOptions(typeof(TSubscriber).GetEventStoreObjectName()));
            return new SubscriberRegistry
                (
                     new Dictionary<string, ConnectSubscriber>
                        {
                            {
                                typeof(TSubscriber).GetEventStoreObjectName(),
                                createConnection =>
                                    SubscriberConnection.ConnectVolatileSubscriber
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
                    new PersistentSubscriberRegistrationOptions(typeof(TSubscriber).GetEventStoreObjectName()));
            return new SubscriberRegistry
                (
                     new Dictionary<string, ConnectSubscriber>
                        {
                            {
                                typeof(TSubscriber).GetEventStoreObjectName(),
                                createConnection =>
                                    SubscriberConnection.ConnectPersistentSubscriber
                                    (
                                        createConnection,
                                        registrationConfiguration.SubscriptionStream,
                                        typeof(TSubscriber).GetEventStoreObjectName(),
                                        handleEvent
                                    )
                            }
                        }
                    .Merge(_subscriberRegistrations)
                );
        }
       */

        public static SubscriberRegistryBuilder CreateSubscriberRegistryBuilder()
        {
            return new SubscriberRegistryBuilder(new Dictionary<string, ISubscriberRegistrationOptions>());
        } 
    }

    /*
    public delegate Task<SubscriberConnection> ConnectSubscriber(Func<IEventStoreConnection> createConnection);

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
			return new CatchupSubscriberRegistrationOptions(typeof(TSubscriptionStream).GetEventStoreObjectName(), GetEventHandlingQueueKey);
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
			return new VolatileSubscriberRegistrationOptions(typeof(TSubscriptionStream).GetEventStoreObjectName());
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
			return new PersistentSubscriberRegistrationOptions(typeof(TSubscriptionStream).GetEventStoreObjectName());
		}
	}

    public struct SubscriberRegistration
    {
        public readonly string Name;
        public readonly ConnectSubscriber Connect;

        public SubscriberRegistration(string name, ConnectSubscriber connect)
        {
            Name = name;
            Connect = connect;
        }
    }

    public struct SubscriberRegistry : IEnumerable<SubscriberRegistration>
    {
        private readonly IDictionary<string, ConnectSubscriber> _subscriberRegistrations;

        private SubscriberRegistry(IDictionary<string, ConnectSubscriber> subscriberRegistrations)
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
                    new CatchupSubscriberRegistrationOptions(typeof(TSubscriber).GetEventStoreObjectName(), resolvedEvent => "default"));
            return new SubscriberRegistry
                (
                    new Dictionary<string, ConnectSubscriber>
                        {
                            {
                                typeof(TSubscriber).GetEventStoreObjectName(),
                                createConnection =>
                                    SubscriberConnection.ConnectCatchUpSubscriber
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
                    new VolatileSubscriberRegistrationOptions(typeof(TSubscriber).GetEventStoreObjectName()));
            return new SubscriberRegistry
                (
                     new Dictionary<string, ConnectSubscriber>
                        {
                            {
                                typeof(TSubscriber).GetEventStoreObjectName(),
                                createConnection =>
                                    SubscriberConnection.ConnectVolatileSubscriber
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
                    new PersistentSubscriberRegistrationOptions(typeof(TSubscriber).GetEventStoreObjectName()));
            return new SubscriberRegistry
                (
                     new Dictionary<string, ConnectSubscriber>
                        {
                            {
                                typeof(TSubscriber).GetEventStoreObjectName(),
                                createConnection =>
                                    SubscriberConnection.ConnectPersistentSubscriber
                                    (
                                        createConnection,
                                        registrationConfiguration.SubscriptionStream,
                                        typeof(TSubscriber).GetEventStoreObjectName(),
                                        handleEvent
                                    )
                            }
                        }
                    .Merge(_subscriberRegistrations)
                );
        }

        public static SubscriberRegistry CreateSubscriberRegistry()
        {
            return new SubscriberRegistry(new Dictionary<string, ConnectSubscriber>());
        }

        public IEnumerator<SubscriberRegistration> GetEnumerator()
        {
            return _subscriberRegistrations
                .Select(x => new SubscriberRegistration(x.Key, x.Value))
                .GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
    */
}
