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

    public sealed class EventBus : IEventBus, ISubscribeRegistrationHandler
	{
        //private readonly TaskQueue _queue = new TaskQueue();
        private readonly ConcurrentDictionary<string, SubscriberConnection> _subscriberConnections = new ConcurrentDictionary<string, SubscriberConnection>();

		private readonly Func<IEventStoreConnection> _createConnection;
		private readonly ISubscriberRegistry _subscriberRegistry;

		private EventBus(Func<IEventStoreConnection> createConnection, ISubscriberRegistry subscriberRegistry)
		{
			_createConnection = createConnection;
			_subscriberRegistry = subscriberRegistry;
			//_subscribers = 
			//    subscriberRegistry
			//        .ToDictionary
			//        (
			//            x => x.Key,
			//            x =>
			//            {
			//                async Task<DisconnectSubscriber> ConnectSubscriber(Action<SubscriptionDropReason> subscriptionDropped)
			//                {
			//                    var connection = await x.Value
			//                    (
			//                     createConnection, (dropReason, exception) => subscriptionDropped(dropReason)
			//                    );
			//                    return () =>
			//                    {
			//                        connection.Disconnect();
			//                        return ConnectSubscriber;
			//                    };
			//                }
			//                return (Delegate)new ConnectSubscriber(ConnectSubscriber);
			//            }
			//        );
		}

        public StopSubscriberResult StopSubscriber(string subscriberName)
        {
            if (!_subscriberRegistry.ContainsKey(subscriberName))
            {
                return StopSubscriberResult.NotFound;
            }
	        if(_subscriberConnections.TryGetValue(subscriberName, out var subscriberConnection))
	        {
		        subscriberConnection.Disconnect();
	        }
	        return StopSubscriberResult.Stopped;
	        //var tsc = new TaskCompletionSource<StopSubscriberResult>();
	        //await _queue.SendToChannel
	        //(
	        //    subscriberName,
	        //    () =>
	        //    {
	        //        DisconnectSubscriber disconnectSubscriber;
	        //        if ((disconnectSubscriber = _subscribers[subscriberName] as DisconnectSubscriber) != null)
	        //        {
	        //            _subscribers[subscriberName] = disconnectSubscriber();
	        //        }
	        //        return Task.CompletedTask;
	        //    },
	        //    taskSucceeded: x => tsc.SetResult(StopSubscriberResult.Stopped)
	        //);
	        //return await tsc.Task;
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
			//var tsc = new TaskCompletionSource<StartSubscriberResult>();
   //         await _queue.SendToChannel
   //         (
   //             subscriberName,
   //             async () =>
   //             {
	                
	  //              //ConnectSubscriber connectSubscriber;
	  //              //if ((connectSubscriber = _subscribers[subscriberName] as ConnectSubscriber) != null)
	  //              //{
	  //              //    _subscribers[subscriberName] = await connectSubscriber
	  //              //    (
	  //              //        async dropReason =>
	  //              //        {
	  //              //            if (dropReason == SubscriptionDropReason.UserInitiated)
	  //              //            {
	  //              //                return;
	  //              //            }
	  //              //            await StopSubscriber(subscriberName);
	  //              //            await StartSubscriber(subscriberName);
	  //              //        }
	  //              //    );
	  //              //}
   //             },
   //             taskSucceeded: x => tsc.SetResult(StartSubscriberResult.Started)
   //         );
   //         return await tsc.Task;
        }

        public Task StartAllSubscribers()
        {
            return Task.WhenAll(_subscriberRegistry.Select(x => StartSubscriber(x.Key)));
        }

	    public static IEventBus CreateEventBus(Func<IEventStoreConnection> createConnection, Func<SubscriberRegistryBuilder, ISubscriberRegistry> setSubscriberRegistry)
	    {
		    return new EventBus(createConnection, setSubscriberRegistry(SubscriberRegistryBuilder.CreateSubscriberRegistryBuilder()));
	    }

		async Task IMessageHandler<CatchUpSubscriberRegistration, Task>.Handle(CatchUpSubscriberRegistration message)
		{
			var subscriberConnection = await SubscriberConnection.ConnectCatchUpSubscriber
				(
					_createConnection, 
					message.SubscriptionStreamName, 
					message.HandleEvent, 
					message.GetCheckpoint, 
					message.GetEventHandlingQueueKey,
					async (dropReason, ex) =>
					{
						if (dropReason == SubscriptionDropReason.UserInitiated)
						{
							return;
						}
						StopSubscriber("");
						await StartSubscriber("");
					}
				);
		}

		async Task IMessageHandler<VolatileSubscriberRegistration, Task>.Handle(VolatileSubscriberRegistration message)
		{
			var subscriberConnection = await SubscriberConnection.ConnectVolatileSubscriber
			(
				_createConnection,
				message.SubscriptionStreamName,
				message.HandleEvent,
				async (dropReason, ex) =>
				{
					if (dropReason == SubscriptionDropReason.UserInitiated)
					{
						return;
					}
					StopSubscriber("");
					await StartSubscriber("");
				}
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
				async (dropReason, ex) =>
				{
					if (dropReason == SubscriptionDropReason.UserInitiated)
					{
						return;
					}
					StopSubscriber("");
					await StartSubscriber("");
				}
			);
		}

		private delegate Task<DisconnectSubscriber> ConnectSubscriber(Action<SubscriptionDropReason> subscriptionDropped);

		private delegate ConnectSubscriber DisconnectSubscriber();
	}
}
