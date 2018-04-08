using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

using EventStore.ClientAPI;
using shared;
using ImpromptuInterface;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;

namespace eventstore
{
    public enum StopSubscriberResult
    {
        NotFound,
        Stopped
    }

    public enum StartSubscriberResult
    {
        NotFound,
        Started
    }

    public interface IEventBus
    {
        Task<StopSubscriberResult> StopSubscriber(string subscriberName);
        Task StopAllSubscribers();
        Task<StartSubscriberResult> StartSubscriber(string subscriberName);
        Task StartAllSubscribers();
    }

    public sealed class EventBus : IEventBus, ISubscriberRegistrationsHandler
	{
        private readonly ConcurrentDictionary<ISubscriberRegistration, Lazy<Task<SubscriberConnection>>> _subscriberConnections = new ConcurrentDictionary<ISubscriberRegistration, Lazy<Task<SubscriberConnection>>>();
        private readonly Func<IEventStoreConnection> _createConnection;
		private readonly ISubscriberRegistry _subscriberRegistry;

		private EventBus(Func<IEventStoreConnection> createConnection, ISubscriberRegistry subscriberRegistry)
		{
			_createConnection = createConnection;
			_subscriberRegistry = subscriberRegistry;
		}

        public async Task<StopSubscriberResult> StopSubscriber(string subscriberName)
        {
            if (!_subscriberRegistry.ContainsKey(subscriberName))
            {
                return StopSubscriberResult.NotFound;
            }
	        if(_subscriberConnections.TryRemove(_subscriberRegistry[subscriberName], out var subscriberConnection))
	        {
                (await subscriberConnection.Value).Disconnect();
            }
	        return StopSubscriberResult.Stopped;
        }

        public Task StopAllSubscribers()
        {
            return Task.WhenAll(_subscriberRegistry.Select(x => StopSubscriber(x.Key)));
        }

        public async Task<StartSubscriberResult> StartSubscriber(string subscriberName)
        {
            if (!_subscriberRegistry.ContainsKey(subscriberName))
            {
                return StartSubscriberResult.NotFound;
            }
			await _subscriberRegistry[subscriberName].Handle(this);
	        return StartSubscriberResult.Started;
        }

        public Task StartAllSubscribers()
        {
            return Task.WhenAll(_subscriberRegistry.Select(x => StartSubscriber(x.Key)));
        }

	    public static IEventBus CreateEventBus(Func<IEventStoreConnection> createConnection, Func<SubscriberRegistryBuilder, ISubscriberRegistry> setSubscriberRegistry)
	    {
		    return new EventBus(createConnection, setSubscriberRegistry(SubscriberRegistryBuilder.CreateSubscriberRegistryBuilder()));
	    }

		Task IMessageHandler<CatchUpSubscriberRegistration, Task>.Handle(CatchUpSubscriberRegistration message)
		{
            var subscriberConnection = _subscriberConnections.GetOrAdd
                (
                    message, 
                    new Lazy<Task<SubscriberConnection>>
                    (
                        () => SubscriberConnection.ConnectCatchUpSubscriber
                            (
                                _createConnection,
                                message.SubscriptionStreamName,
                                message.HandleEvent,
                                message.GetCheckpoint,
                                message.GetEventHandlingQueueKey,
                                RestartSubscriber("")
                            )
                    )
                );
            return subscriberConnection.Value;
        }

		async Task IMessageHandler<VolatileSubscriberRegistration, Task>.Handle(VolatileSubscriberRegistration message)
		{
			var subscriberConnection = await SubscriberConnection.ConnectVolatileSubscriber
			(
				_createConnection,
				message.SubscriptionStreamName,
				message.HandleEvent,
                RestartSubscriber("")
            );
		}

		async Task IMessageHandler<PersistentSubscriberRegistration, Task>.Handle(PersistentSubscriberRegistration message)
		{
			var subscriberConnection = await SubscriberConnection.ConnectPersistentSubscriber
			(
				_createConnection,
				message.SubscriptionStreamName,
				message.SubscriptionGroupName,
				message.HandleEvent,
                RestartSubscriber("")
            );
		}

        private Action<SubscriptionDropReason, Exception> RestartSubscriber(string subscriberName)
        {
            return async (dropReason, ex) =>
            {
                if (dropReason == SubscriptionDropReason.UserInitiated)
                {
                    return;
                }
                await StopSubscriber("");
                await StartSubscriber("");
            };
        }

        

    }
}
