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

	public sealed class EventBus
    {
        private readonly TaskQueue _queue = new TaskQueue();
        private readonly IDictionary<string, Delegate> _subscriberOperations;

        private EventBus
            (
				Func<IEventStoreConnection> createConnection, 
				SubscriberRegistry subscriberRegistry
			)
        {
            _subscriberOperations = subscriberRegistry
	            .ToDictionary
	            (
		            x => x.Name,
		            x =>
		            {
			            async Task<Disconnect> Connect()
			            {
				            var connection = await x.Connect(createConnection);
				            return () =>
				            {
					            connection.Disconnect();
					            return Connect;
				            };
			            }
			            return (Delegate)new Connect(Connect);
		            }
	            );
        }

        public async Task<StopSubscriberResult> StopSubscriber(string subscriberName)
        {
            if (!_subscriberOperations.ContainsKey(subscriberName))
            {
                return StopSubscriberResult.NotFound;
            }

            var tsc = new TaskCompletionSource<StopSubscriberResult>();
            await _queue.SendToChannel
                (
                    () =>
                    {
                        Disconnect operation;
                        if ((operation = _subscriberOperations[subscriberName] as Disconnect) != null)
                        {
                            _subscriberOperations[subscriberName] = operation();
                        }
                        return Task.CompletedTask;
                    },
                    channelName: subscriberName,
                    taskSucceeded: x => tsc.SetResult(StopSubscriberResult.Stopped)
                );
            return await tsc.Task;
        }

        public Task StopAllSubscribers()
        {
            return Task.WhenAll(_subscriberOperations.Select(x => StopSubscriber(x.Key)));
        }

        public async Task<StartSubscriberResult> StartSubscriber(string subscriberName)
        {
            if (!_subscriberOperations.ContainsKey(subscriberName))
            {
                return StartSubscriberResult.NotFound;
            }

            var tsc = new TaskCompletionSource<StartSubscriberResult>();
            await _queue.SendToChannel
                (
                    async () =>
                    {
                        Connect operation;
                        if ((operation = _subscriberOperations[subscriberName] as Connect) != null)
                        {
                            _subscriberOperations[subscriberName] = await operation();
                        }
                    },
                    channelName: subscriberName,
                    taskSucceeded: x => tsc.SetResult(StartSubscriberResult.Started)
                );
            return await tsc.Task;
        }

        public Task StartAllSubscribers()
        {
            return Task.WhenAll(_subscriberOperations.Select(x => StartSubscriber(x.Key)));
        }

        public static EventBus CreateEventBus(Func<IEventStoreConnection> createConnection, Func<SubscriberRegistry, SubscriberRegistry> configureSubscriberRegistry)
        {
            var subscriberRegistry = configureSubscriberRegistry(SubscriberRegistry.CreateSubscriberRegistry());
            return new EventBus
                (
					createConnection,
					subscriberRegistry
				);
        }

        private delegate Task<Disconnect> Connect();

        private delegate Connect Disconnect(); 
    }
}
