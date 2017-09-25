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

        public EventBus RegisterCatchupSubscriber<TSubscription>(Func<ResolvedEvent, Task<ResolvedEvent>> handleResolvedEvent, Func<Task<long?>> getCheckpoint, Func<ResolvedEvent, string> getEventHandlingQueueKey = null) where TSubscription : IMessageHandler
        {
            //todo:move queue to CatchUpSubscriber
            var queue = new TaskQueue();
           
            return new EventBus(_createConnection,
                _subscriptions.Concat(new Func<Task>[]
                {
                    new CatchUpSubscriber(
                        _createConnection,
                        typeof(TSubscription).GetEventStoreName(),                     
                        (subscription, resolvedEvent) => 
                            queue.SendToChannelAsync(
                                getEventHandlingQueueKey == null ? string.Empty : getEventHandlingQueueKey(resolvedEvent), 
                                () => HandleResolvedEvent(handleResolvedEvent, delegate { }, delegate { }, subscription, resolvedEvent)),
                        TimeSpan.FromSeconds(1),
                        getCheckpoint)
                        .Start
                }));
        }

        //public EventBus RegisterVolatileSubscriber<TSubscription>(TSubscription subscriber,
        //	Func<Func<ResolvedEvent, Task<ResolvedEvent>>, Func<ResolvedEvent, Task<ResolvedEvent>>> processEventHandling = null) where TSubscription : IMessageHandler
        //{
        //	return RegisterVolatileSubscriber<TSubscription, TSubscription>(subscriber, processEventHandling);
        //}

        //public EventBus RegisterVolatileSubscriber<TSubscription, TSubscriber>(TSubscriber subscriber,
        //	Func<Func<ResolvedEvent, Task<ResolvedEvent>>, Func<ResolvedEvent, Task<ResolvedEvent>>> processEventHandling = null) where TSubscriber : TSubscription where TSubscription : IMessageHandler
        //{
        //	async Task<ResolvedEvent> SubscriberResolvedEventHandler(ResolvedEvent resolvedEvent)
        //	{
        //		var handleResolvedEvent = subscriber.CreateResolvedEventHandler<Task>();
        //		await handleResolvedEvent(resolvedEvent);
        //		return resolvedEvent;
        //	}
        //	processEventHandling = processEventHandling ?? (x => x);

        //	return new EventBus(_createConnection,
        //		_subscriptions.Concat(new Func<Task>[]
        //		{
        //			new VolatileSubscriber(
        //				_createConnection,
        //				typeof(TSubscription).GetEventStoreName(),
        //				(subscription, resolvedEvent) =>
        //					processEventHandling(
        //							CreateSubscriptionResolvedEventHandler(
        //								subscription,
        //								SubscriberResolvedEventHandler))
        //						(resolvedEvent),
        //				TimeSpan.FromSeconds(1))
        //				.Start
        //		}));
        //}

        //public EventBus RegisterPersistentSubscriber<TSubscription>(TSubscription subscriber,
        //	Func<Func<ResolvedEvent, Task<ResolvedEvent>>, Func<ResolvedEvent, Task<ResolvedEvent>>> processEventHandling = null) where TSubscription : IMessageHandler
        //{
        //	return RegisterPersistentSubscriber<TSubscription, TSubscription>(subscriber, processEventHandling);
        //}

        //public EventBus RegisterPersistentSubscriber<TSubscription, TSubscriber>(TSubscriber subscriber,
        //	Func<Func<ResolvedEvent, Task<ResolvedEvent>>, Func<ResolvedEvent, Task<ResolvedEvent>>> processEventHandling = null) where TSubscriber : TSubscription where TSubscription : IMessageHandler
        //{
        //	async Task<ResolvedEvent> SubscriberResolvedEventHandler(ResolvedEvent resolvedEvent)
        //	{
        //		var handleResolvedEvent = subscriber.CreateResolvedEventHandler<Task>();
        //		await handleResolvedEvent(resolvedEvent);
        //		return resolvedEvent;
        //	}
        //	processEventHandling = processEventHandling ?? (x => x);

        //	void EventHandlingSucceeded(EventStorePersistentSubscriptionBase subscription, ResolvedEvent resolvedEvent)
        //	{
        //		subscription.Acknowledge(resolvedEvent);
        //	}

        //	void EventHandlingFailed(EventStorePersistentSubscriptionBase subscription, ResolvedEvent resolvedEvent, Exception exception)
        //	{
        //		subscription.Fail(resolvedEvent, PersistentSubscriptionNakEventAction.Unknown, exception.Message);
        //	}

        //	return new EventBus(_createConnection,
        //		_subscriptions.Concat(new Func<Task>[]
        //		{
        //			new PersistentSubscriber(
        //				_createConnection,
        //				typeof(TSubscription).GetEventStoreName(),
        //				typeof(TSubscriber).GetEventStoreName(),
        //				(subscription, resolvedEvent) =>
        //					processEventHandling(
        //							CreateSubscriptionResolvedEventHandler(
        //								subscription,
        //								SubscriberResolvedEventHandler, 
        //								EventHandlingSucceeded,
        //								EventHandlingFailed))
        //						(resolvedEvent),
        //				TimeSpan.FromSeconds(1))
        //				.Start
        //		}));
        //}

        public void Start()
        {
            Parallel.ForEach(_subscriptions, start => start());
        }


        private static async Task HandleResolvedEvent<TSubscription>(
	        Func<ResolvedEvent, Task<ResolvedEvent>> handleResolvedEvent,
            Action<TSubscription, ResolvedEvent> eventHandlingSucceeded,
            Action<TSubscription, ResolvedEvent, Exception> eventHandlingFailed,
            TSubscription subscription,
            ResolvedEvent resolvedEvent)
        {
            try
            {
                await handleResolvedEvent(resolvedEvent);
            }
            catch (Exception ex)
            {
                eventHandlingFailed(subscription, resolvedEvent, ex);
                return;
            }
            eventHandlingSucceeded(subscription, resolvedEvent);
        }
    }
}
