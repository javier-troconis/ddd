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
        StopSubscriberResult StopSubscriber(string subscriberName);
        void StopAllSubscribers();
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

        public StopSubscriberResult StopSubscriber(string subscriberName)
        {
            if (!_subscriberRegistry.ContainsKey(subscriberName))
            {
                return StopSubscriberResult.NotFound;
            }
	        if(_subscriberConnections.TryRemove(_subscriberRegistry[subscriberName], out var subscriberConnection))
	        {
		        subscriberConnection.Value.Disconnect();
	        }
	        return StopSubscriberResult.Stopped;
        }

        public void StopAllSubscribers()
        {
	        foreach (var subscriberRegistration in _subscriberRegistry)
	        {
		        StopSubscriber(subscriberRegistration.Key);
	        }
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
            
			//SubscriberConnection connection;
			//if(_subscriberConnections.
			//(
			//	message,
			//	k =>
			//	{
					
			//	},

			//	//SubscriberConnection.ConnectCatchUpSubscriber
			//	//(
			//	//	_createConnection,
			//	//	message.SubscriptionStreamName,
			//	//	message.HandleEvent,
			//	//	message.GetCheckpoint,
			//	//	message.GetEventHandlingQueueKey,
			//	//	async (dropReason, ex) =>
			//	//	{
			//	//		if (dropReason == SubscriptionDropReason.UserInitiated)
			//	//		{
			//	//			return;
			//	//		}
			//	//		StopSubscriber("");
			//	//		await StartSubscriber("");
			//	//	}
			//	//)
			//);


			//if (_subscriberConnections.TryGetValue(message, out SubscriberConnection c))
			//{
				
			//}

			var subscriberConnection = new Lazy<Task<SubscriberConnection>>
            (
                () => SubscriberConnection.ConnectCatchUpSubscriber
			        (
				        _createConnection, 
				        message.SubscriptionStreamName, 
				        message.HandleEvent, 
				        message.GetCheckpoint, 
				        message.GetEventHandlingQueueKey,
                        OnSubcriptionDropped("")
                    )
            );
		}

		async Task IMessageHandler<VolatileSubscriberRegistration, Task>.Handle(VolatileSubscriberRegistration message)
		{
			var subscriberConnection = await SubscriberConnection.ConnectVolatileSubscriber
			(
				_createConnection,
				message.SubscriptionStreamName,
				message.HandleEvent,
                OnSubcriptionDropped("")
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
                OnSubcriptionDropped("")
            );
		}

        private Action<SubscriptionDropReason, Exception> OnSubcriptionDropped(string subscriberName)
        {
            return async (dropReason, ex) =>
            {
                if (dropReason == SubscriptionDropReason.UserInitiated)
                {
                    return;
                }
                StopSubscriber("");
                await StartSubscriber("");
            };
        }

    }
}
