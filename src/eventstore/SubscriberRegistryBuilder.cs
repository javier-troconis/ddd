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
    public interface ICatchupSubscriberRegistrationOptions
    {
        string SubscriptionStreamName { get; }
        string SubscriberName { get; }
        Func<ResolvedEvent, string> GetEventHandlingQueueName { get; }
	    Func<Func<ResolvedEvent, Task<ResolvedEvent>>, Func<ResolvedEvent, Task>> ProcessEventHandling { get; }
	}

    public class CatchupSubscriberRegistrationOptions : ICatchupSubscriberRegistrationOptions
    {
        internal CatchupSubscriberRegistrationOptions(EventStoreObjectName subscriptionStreamName, string subscriberName, Func<ResolvedEvent, string> getEventHandlingQueueName, Func<Func<ResolvedEvent, Task<ResolvedEvent>>, Func<ResolvedEvent, Task>> processEventHandling)
        {
            SubscriptionStreamName = subscriptionStreamName;
            SubscriberName = subscriberName;
            GetEventHandlingQueueName = getEventHandlingQueueName;
	        ProcessEventHandling = processEventHandling;
        }

        public CatchupSubscriberRegistrationOptions SetEventHandlingQueueKey(Func<ResolvedEvent, string> getEventHandlingQueueKey)
        {
            return new CatchupSubscriberRegistrationOptions(SubscriptionStreamName, SubscriberName, getEventHandlingQueueKey, ProcessEventHandling);
        }

        public CatchupSubscriberRegistrationOptions SetSubscriptionStreamName(EventStoreObjectName streamName)
        {
            return new CatchupSubscriberRegistrationOptions(streamName, SubscriberName, GetEventHandlingQueueName, ProcessEventHandling);
        }

        public CatchupSubscriberRegistrationOptions SetSubscriberName(string subscriberName)
        {
            return new CatchupSubscriberRegistrationOptions(SubscriptionStreamName, subscriberName, GetEventHandlingQueueName, ProcessEventHandling);
        }

        public CatchupSubscriberRegistrationOptions SetSubscriberNamingConvention(Func<string, string> subscriberNamingConvention)
        {
            return new CatchupSubscriberRegistrationOptions(SubscriptionStreamName, subscriberNamingConvention(SubscriberName), GetEventHandlingQueueName, ProcessEventHandling);
        }

	    public CatchupSubscriberRegistrationOptions SetEventHandlingProcessor(Func<Func<ResolvedEvent, Task<ResolvedEvent>>, Func<ResolvedEvent, Task>> processEventHandling)
	    {
			return new CatchupSubscriberRegistrationOptions(SubscriptionStreamName, SubscriberName, GetEventHandlingQueueName, processEventHandling);
		}

        public string SubscriptionStreamName { get; }
        public string SubscriberName { get; }
        public Func<ResolvedEvent, string> GetEventHandlingQueueName { get; }
		public Func<Func<ResolvedEvent, Task<ResolvedEvent>>, Func<ResolvedEvent, Task>> ProcessEventHandling { get; }
	}

    public interface IVolatileSubscriberRegistrationOptions
    {
        string SubscriptionStreamName { get; }
        string SubscriberName { get; }
	    Func<Func<ResolvedEvent, Task<ResolvedEvent>>, Func<ResolvedEvent, Task>> ProcessEventHandling { get; }
	}

    public class VolatileSubscriberRegistrationOptions : IVolatileSubscriberRegistrationOptions
    {
        internal VolatileSubscriberRegistrationOptions(EventStoreObjectName subscriptionStreamName, string subscriberName, Func<Func<ResolvedEvent, Task<ResolvedEvent>>, Func<ResolvedEvent, Task>> processEventHandling)
        {
            SubscriptionStreamName = subscriptionStreamName;
            SubscriberName = subscriberName;
	        ProcessEventHandling = processEventHandling;
        }

        public VolatileSubscriberRegistrationOptions SetSubscriptionStreamName(EventStoreObjectName streamName)
        {
            return new VolatileSubscriberRegistrationOptions(streamName, SubscriberName, ProcessEventHandling);
        }

        public VolatileSubscriberRegistrationOptions SetSubscriberName(string subscriberName)
        {
            return new VolatileSubscriberRegistrationOptions(SubscriptionStreamName, subscriberName, ProcessEventHandling);
        }

        public VolatileSubscriberRegistrationOptions SetSubscriberNamingConvention(Func<string, string> subscriberNamingConvention)
        {
            return new VolatileSubscriberRegistrationOptions(SubscriptionStreamName, subscriberNamingConvention(SubscriberName), ProcessEventHandling);
        }

	    public VolatileSubscriberRegistrationOptions SetEventHandlingProcessor(Func<Func<ResolvedEvent, Task<ResolvedEvent>>, Func<ResolvedEvent, Task>> processEventHandling)
	    {
		    return new VolatileSubscriberRegistrationOptions(SubscriptionStreamName, SubscriberName, processEventHandling);
	    }

		public string SubscriptionStreamName { get; }
        public string SubscriberName { get; }
		public Func<Func<ResolvedEvent, Task<ResolvedEvent>>, Func<ResolvedEvent, Task>> ProcessEventHandling { get; }
	}

    public interface IPersistentSubscriberRegistrationOptions
    {
        string SubscriberName { get; }
        string SubscriptionStreamName { get; }
        string SubscriptionGroupName { get; }
	    Func<Func<ResolvedEvent, Task<ResolvedEvent>>, Func<ResolvedEvent, Task>> ProcessEventHandling { get; }
	}

    public class PersistentSubscriberRegistrationOptions : IPersistentSubscriberRegistrationOptions
    {
        internal PersistentSubscriberRegistrationOptions(EventStoreObjectName subscriptionStreamName, EventStoreObjectName subscriptionGroupName, string subscriberName, Func<Func<ResolvedEvent, Task<ResolvedEvent>>, Func<ResolvedEvent, Task>> processEventHandling)
        {
            SubscriptionStreamName = subscriptionStreamName;
            SubscriptionGroupName = subscriptionGroupName;
            SubscriberName = subscriberName;
	        ProcessEventHandling = processEventHandling;
        }

        public PersistentSubscriberRegistrationOptions SetSubscriptionStreamName(EventStoreObjectName streamName)
        {
            return new PersistentSubscriberRegistrationOptions(streamName, SubscriptionGroupName, SubscriberName, ProcessEventHandling);
        }

        public PersistentSubscriberRegistrationOptions SetSubscriptionGroupName(EventStoreObjectName subscriptionGroupName)
        {
            return new PersistentSubscriberRegistrationOptions(SubscriptionStreamName, subscriptionGroupName, SubscriberName, ProcessEventHandling);
        }

        public PersistentSubscriberRegistrationOptions SetSubscriberName(string subscriberName)
        {
            return new PersistentSubscriberRegistrationOptions(SubscriptionStreamName, SubscriptionGroupName, subscriberName, ProcessEventHandling);
        }

        public PersistentSubscriberRegistrationOptions SetSubscriberNamingConvention(Func<string, string> subscriberNamingConvention)
        {
            return new PersistentSubscriberRegistrationOptions(SubscriptionStreamName, SubscriptionGroupName, subscriberNamingConvention(SubscriberName), ProcessEventHandling);
        }

	    public PersistentSubscriberRegistrationOptions SetEventHandlingProcessor(Func<Func<ResolvedEvent, Task<ResolvedEvent>>, Func<ResolvedEvent, Task>> processEventHandling)
	    {
			return new PersistentSubscriberRegistrationOptions(SubscriptionStreamName, SubscriptionGroupName, SubscriberName, processEventHandling);
		}

		public string SubscriberName { get; }
        public string SubscriptionStreamName { get; }
        public string SubscriptionGroupName { get; }
		public Func<Func<ResolvedEvent, Task<ResolvedEvent>>, Func<ResolvedEvent, Task>> ProcessEventHandling { get; }
	}

    public class SubscriberRegistryBuilder : ReadOnlyDictionary<string, ISubscriberRegistration>, ISubscriberRegistry
    {
        private SubscriberRegistryBuilder(IDictionary<string, ISubscriberRegistration> dictionary) : base(dictionary)
        {
          
        }

        
        public SubscriberRegistryBuilder RegisterCatchupSubscriber<TSubscriber>(TSubscriber subscriber, Func<Task<long?>> getCheckpoint, Func<CatchupSubscriberRegistrationOptions, ICatchupSubscriberRegistrationOptions> setRegistrationOptions = null) where TSubscriber : IMessageHandler
        {
	        var registrationOptions = new CatchupSubscriberRegistrationOptions(typeof(TSubscriber), (EventStoreObjectName)typeof(TSubscriber), resolvedEvent => string.Empty, x => x).PipeForward(setRegistrationOptions ?? (x => x));
	        var handleEvent = ResolvedEventHandleFactory.CreateResolvedEventHandle<TSubscriber, Task>(delegate { return Task.CompletedTask; });
			return RegisterCatchupSubscriber
            (
				registrationOptions.SubscriberName,
				registrationOptions.SubscriptionStreamName,
                registrationOptions.ProcessEventHandling
                (
                    async resolvedEvent =>
                    {
	                    await handleEvent(subscriber, resolvedEvent);
	                    return resolvedEvent;
                    }
                ),
                getCheckpoint,
				registrationOptions.GetEventHandlingQueueName
			);
        }

        public SubscriberRegistryBuilder RegisterCatchupSubscriber(string subscriberName, EventStoreObjectName subscriptionStreamName, Func<ResolvedEvent, Task> handleEvent, Func<Task<long?>> getCheckpoint, Func<ResolvedEvent, string> getEventHandlingQueueKey = null)
        {
            return new SubscriberRegistryBuilder
                (
                    Dictionary.Merge
                    (
                        new Dictionary<string, ISubscriberRegistration>
                            {
                                {
                                    subscriberName,
									new CatchUpSubscriberRegistration
									(
										subscriptionStreamName,
										handleEvent,
										getCheckpoint,
										getEventHandlingQueueKey ?? (x => string.Empty)
									)
								}
                            }
                    )
                );
        }
        
        public SubscriberRegistryBuilder RegisterVolatileSubscriber<TSubscriber>(TSubscriber subscriber, Func<VolatileSubscriberRegistrationOptions, IVolatileSubscriberRegistrationOptions> setRegistrationOptions = null) where TSubscriber : IMessageHandler
        {
			var registrationOptions = new VolatileSubscriberRegistrationOptions(typeof(TSubscriber), (EventStoreObjectName)typeof(TSubscriber), x => x).PipeForward(setRegistrationOptions ?? (x => x));
	        var handleEvent = ResolvedEventHandleFactory.CreateResolvedEventHandle<TSubscriber, Task>(delegate { return Task.CompletedTask; });
			return RegisterVolatileSubscriber
            (
				registrationOptions.SubscriberName,
				registrationOptions.SubscriptionStreamName,
				registrationOptions.ProcessEventHandling
                (
                    async resolvedEvent =>
				    {
					    await handleEvent(subscriber, resolvedEvent);
					    return resolvedEvent;
				    }
                )
			);
        }

        public SubscriberRegistryBuilder RegisterVolatileSubscriber(string subscriberName, EventStoreObjectName subscriptionStreamName, Func<ResolvedEvent, Task> handleEvent) 
        {
			return new SubscriberRegistryBuilder
			(
				Dictionary.Merge
				(
					new Dictionary<string, ISubscriberRegistration>
					{
						{
							subscriberName,
							new VolatileSubscriberRegistration
							(
								subscriptionStreamName,
								handleEvent
							)
						}
					}
				)
			);
		} 
		
		


        public SubscriberRegistryBuilder RegisterPersistentSubscriber<TSubscriber>(TSubscriber subscriber, Func<PersistentSubscriberRegistrationOptions, IPersistentSubscriberRegistrationOptions> setRegistrationOptions = null) where TSubscriber : IMessageHandler
        {
			var registrationOptions = new PersistentSubscriberRegistrationOptions(typeof(TSubscriber), typeof(TSubscriber), (EventStoreObjectName)typeof(TSubscriber), x => x).PipeForward(setRegistrationOptions ?? (x => x));
	        var handleEvent = ResolvedEventHandleFactory.CreateResolvedEventHandle<TSubscriber, Task>(delegate { return Task.CompletedTask; });
			return RegisterPersistentSubscriber
                (
					registrationOptions.SubscriberName,
					registrationOptions.SubscriptionStreamName,
					registrationOptions.SubscriptionGroupName,
					registrationOptions.ProcessEventHandling
                    (
                        async resolvedEvent =>
					    {
						    await handleEvent(subscriber, resolvedEvent);
						    return resolvedEvent;
					    }
                    )
				);
        }

        public SubscriberRegistryBuilder RegisterPersistentSubscriber(string subscriberName, EventStoreObjectName subscriptionStreamName, EventStoreObjectName subscriptionGroupName, Func<ResolvedEvent, Task> handleEvent)
        {
			return new SubscriberRegistryBuilder
			(
				Dictionary.Merge
				(
					new Dictionary<string, ISubscriberRegistration>
					{
						{
							subscriberName,
							new PersistentSubscriberRegistration
							(
								subscriptionStreamName,
								subscriptionGroupName,
								handleEvent
							)
						}
					}
				)
			);
		}

        internal static SubscriberRegistryBuilder CreateSubscriberRegistryBuilder()
        {
            return new SubscriberRegistryBuilder(new Dictionary<string, ISubscriberRegistration>());
        } 
    }
}
