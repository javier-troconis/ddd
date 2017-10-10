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

	    public EventBus RegisterCatchupSubscriber<TSubscriber>(TSubscriber subscriber, Func<Task<long?>> getCheckpoint, Func<ResolvedEvent, string> getEventHandlingQueueKey = null) where TSubscriber : IMessageHandler
	    {
		    return RegisterCatchupSubscriber<TSubscriber, TSubscriber>(subscriber, getCheckpoint, getEventHandlingQueueKey);

	    }

	    public EventBus RegisterCatchupSubscriber<TSubscription, TSubscriber>(TSubscriber subscriber, Func<Task<long?>> getCheckpoint, Func<ResolvedEvent, string> getEventHandlingQueueKey = null) where TSubscription : IMessageHandler where TSubscriber : TSubscription
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

	    public EventBus RegisterCatchupSubscriber<TSubscription>(Func<ResolvedEvent, Task> handleResolvedEvent, Func<Task<long?>> getCheckpoint, Func<ResolvedEvent, string> getEventHandlingQueueKey = null) where TSubscription : IMessageHandler
        {  
            return new EventBus(_createConnection,
                _subscriptions.Concat(new Func<Task>[]
                {
                    new CatchUpSubscriber
					(
                        _createConnection,
                        typeof(TSubscription).GetEventStoreName(),
                        handleResolvedEvent,
						getCheckpoint,
                        getEventHandlingQueueKey ?? (resolvedEvent => string.Empty),
                        TimeSpan.FromSeconds(1)
                    ).Start
                }));
        }

	    public EventBus RegisterVolatileSubscriber<TSubscriber>(TSubscriber subscriber) where TSubscriber : IMessageHandler
	    {
		    return RegisterVolatileSubscriber<TSubscriber, TSubscriber>(subscriber);
	    }

	    public EventBus RegisterVolatileSubscriber<TSubscription, TSubscriber>(TSubscriber subscriber) where TSubscription : IMessageHandler where TSubscriber : TSubscription
		{
			return RegisterVolatileSubscriber<TSubscription>
				(
					SubscriberResolvedEventHandleFactory
						.CreateSubscriberResolvedEventHandle<TSubscriber, Task>(delegate { return Task.CompletedTask; })
						.Partial(subscriber)
				);
		}

		public EventBus RegisterVolatileSubscriber<TSubscription>(Func<ResolvedEvent, Task> handleResolvedEvent) where TSubscription : IMessageHandler
		{
			return new EventBus(_createConnection,
				_subscriptions.Concat(new Func<Task>[]
				{
					new VolatileSubscriber(
						_createConnection,
						typeof(TSubscription).GetEventStoreName(),
						handleResolvedEvent,
						TimeSpan.FromSeconds(1))
						.Start
				}));
		}

	    public EventBus RegisterPersistentSubscriber<TSubscriber>(TSubscriber subscriber) where TSubscriber : IMessageHandler
	    {
		    return RegisterPersistentSubscriber<TSubscriber, TSubscriber>(subscriber);
	    }

	    public EventBus RegisterPersistentSubscriber<TSubscription, TSubscriber>(TSubscriber subscriber) where TSubscription : IMessageHandler where TSubscriber : TSubscription
		{
			return RegisterPersistentSubscriber<TSubscription, TSubscriber>
			(
				SubscriberResolvedEventHandleFactory
					.CreateSubscriberResolvedEventHandle<TSubscriber, Task>(delegate { return Task.CompletedTask; })
					.Partial(subscriber)
			);
		}

		public EventBus RegisterPersistentSubscriber<TSubscription, TSubscriber>(Func<ResolvedEvent, Task> handleResolvedEvent) where TSubscription : IMessageHandler
		{
			return new EventBus(_createConnection,
				_subscriptions.Concat(new Func<Task>[]
				{
					new PersistentSubscriber(
						_createConnection,
						typeof(TSubscription).GetEventStoreName(),
						typeof(TSubscriber).GetEventStoreName(),
						handleResolvedEvent,
						TimeSpan.FromSeconds(1))
						.Start
				}));
		}

		public void Start()
        {
            Parallel.ForEach(_subscriptions, start => start());
        }


   
    }
}
