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
	public enum ConnectionStatus
	{
		Connected,
		Disconnected
	}

	public class SubscriberStatus
	{
		public readonly string Name;
		public readonly ConnectionStatus ConnectionStatus;

		public SubscriberStatus(string name, ConnectionStatus connectionStatus)
		{
			Name = name;
			ConnectionStatus = connectionStatus;
		}
	}

	public sealed class EventBus
	{
        private struct Subscriber
        {
            private readonly Func<Task<Action>> _start;
            private Action _stop;
            private ConnectionStatus _status;

            public Subscriber(Func<Task<Action>> start)
            {
                _start = start;
                _stop = () => { };
                _status = ConnectionStatus.Disconnected;
            }

            public async Task Start()
            {
                _stop = await _start();
                _status = ConnectionStatus.Connected;
            }

            public void Stop()
            {
                _stop();
                _status = ConnectionStatus.Disconnected;
            }

            public ConnectionStatus Status
            {
                get
                {
                    return _status;
                }
            }
        }

        private readonly TaskQueue _queue = new TaskQueue();
		private readonly Func<IEventStoreConnection> _createConnection;
        private readonly IReadOnlyDictionary<string, Subscriber> _subscribers;

		private EventBus(
            Func<IEventStoreConnection> createConnection, 
            IReadOnlyDictionary<string, Subscriber> subscribers)
		{
			_createConnection = createConnection;
            _subscribers = subscribers;
		}

		public async Task<SubscriberStatus> StopSubscriber(string subscriberName)
		{
            if (!_subscribers.TryGetValue(subscriberName, out Subscriber subscriber))
            {
                return await Task.FromResult(default(SubscriberStatus));
            }

            var tsc = new TaskCompletionSource<SubscriberStatus>();

            await _queue.SendToChannel
                (
                    () =>
                    {
                        if (_subscribers[subscriberName].Status == ConnectionStatus.Connected)
                        {
                            _subscribers[subscriberName].Stop();
                        }
                        return Task.CompletedTask;
                    },
                    channelName: subscriberName,
                    taskSucceeded: x => tsc.SetResult(new SubscriberStatus(subscriberName, ConnectionStatus.Disconnected))
                );
            return await tsc.Task;
        }

        public async Task<IEnumerable<SubscriberStatus>> StopAllSubscribers()
		{
            return await Task.WhenAll(_subscribers.Select(x => StopSubscriber(x.Key)));
        }

		public async Task<SubscriberStatus> StartSubscriber(string subscriberName)
		{
            if (!_subscribers.TryGetValue(subscriberName, out Subscriber subscriber))
            {
                return await Task.FromResult(default(SubscriberStatus));
            }

            var tsc = new TaskCompletionSource<SubscriberStatus>();

            await _queue.SendToChannel
                (
                    async () =>
                    {
                        if (_subscribers[subscriberName].Status == ConnectionStatus.Disconnected)
                        {
                            await _subscribers[subscriberName].Start();
                        }
                    },
                    channelName: subscriberName,
                    taskSucceeded: x => tsc.SetResult(new SubscriberStatus(subscriberName, ConnectionStatus.Connected))
                );
            return await tsc.Task;
        }

		public async Task<IEnumerable<SubscriberStatus>> StartAllSubscribers()
		{
            return await Task.WhenAll(_subscribers.Select(x => StartSubscriber(x.Key)));
        }

		public static EventBus CreateEventBus(Func<IEventStoreConnection> createConnection, Func<SubscriberRegistry, SubscriberRegistry> configureSubscribersRegistry)
		{
            var subscriberRegistry = configureSubscribersRegistry(SubscriberRegistry.CreateSubscriberRegistry());
            var subscribers = subscriberRegistry
                .ToReadOnlyDictionary
                (
                    x => x.SubscriberName, 
                    x => new Subscriber
                    (
                        async () =>
                        {
                            var subscriber = await x.StartSubscriber(createConnection);
                            return subscriber.Stop;
                        }
                    )
                );
            return new EventBus(createConnection, subscribers);
		}
	}
}
