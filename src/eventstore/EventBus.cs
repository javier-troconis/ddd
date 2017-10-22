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
        Unknown,
		Connected,
		NotConnected
	}

    public sealed class EventBus
    {
        private readonly TaskQueue _queue = new TaskQueue();
        private readonly Func<IEventStoreConnection> _createConnection;
        private readonly IReadOnlyDictionary<string, StartSubscriber> _subscriberRegistry;
        private readonly IDictionary<string, ISubscriber> _subscribers;

        private EventBus
            (
                Func<IEventStoreConnection> createConnection,
                IReadOnlyDictionary<string, StartSubscriber> subscriberRegistry,
                IDictionary<string, ISubscriber> subscribers
            )
        {
            _createConnection = createConnection;
            _subscriberRegistry = subscriberRegistry;
            _subscribers = subscribers;
        }

        public async Task<ConnectionStatus> StopSubscriber(string subscriberName)
        {
            if (!_subscribers.ContainsKey(subscriberName))
            {
                return ConnectionStatus.Unknown;
            }

            var tsc = new TaskCompletionSource<ConnectionStatus>();
            await _queue.SendToChannel
                (
                    () =>
                    {
                        ConnectedSubscriber connectedSubscriber;
                        if ((connectedSubscriber = _subscribers[subscriberName] as ConnectedSubscriber) != null)
                        {
                            connectedSubscriber.Stop();
                            _subscribers[subscriberName] = new NotConnectedSubscriber(_subscriberRegistry[subscriberName]);
                        }
                        return Task.CompletedTask;
                    },
                    channelName: subscriberName,
                    taskSucceeded: x => tsc.SetResult(ConnectionStatus.NotConnected)
                );
            return await tsc.Task;
        }

        public Task StopAllSubscribers()
        {
            return Task.WhenAll(_subscribers.Select(x => StopSubscriber(x.Key)));
        }

        public async Task<ConnectionStatus> StartSubscriber(string subscriberName)
        {
            if (!_subscribers.ContainsKey(subscriberName))
            {
                return ConnectionStatus.Unknown;
            }

            var tsc = new TaskCompletionSource<ConnectionStatus>();
            await _queue.SendToChannel
                (
                    async () =>
                    {
                        NotConnectedSubscriber notConnectedSubscriber;
                        if ((notConnectedSubscriber = _subscribers[subscriberName] as NotConnectedSubscriber) != null)
                        {
                            var connectedSubscriber = await notConnectedSubscriber.Start(_createConnection);
                            _subscribers[subscriberName] = new ConnectedSubscriber(connectedSubscriber.Stop);
                        }
                        
                    },
                    channelName: subscriberName,
                    taskSucceeded: x => tsc.SetResult(ConnectionStatus.Connected)
                );
            return await tsc.Task;
        }

        public  Task StartAllSubscribers()
        {
            return Task.WhenAll(_subscribers.Select(x => StartSubscriber(x.Key)));
        }

        public static EventBus CreateEventBus(Func<IEventStoreConnection> createConnection, Func<SubscriberRegistry, SubscriberRegistry> configureSubscribersRegistry)
        {
            var subscriberRegistry = configureSubscribersRegistry(SubscriberRegistry.CreateSubscriberRegistry());

            return new EventBus
                (
                    createConnection,
                    subscriberRegistry
                        .ToReadOnlyDictionary
                        (
                            x => x.SubscriberName,
                            x => x.StartSubscriber
                        ),
                    subscriberRegistry
                        .ToDictionary
                        (
                            x => x.SubscriberName,
                            x => (ISubscriber)new NotConnectedSubscriber
                            (
                                x.StartSubscriber
                            )
                        )
                );
        }

        private interface ISubscriber
        {

        }

        private class ConnectedSubscriber : ISubscriber
        {
            public readonly Action Stop;

            public ConnectedSubscriber(Action stop)
            {
                Stop = stop;
            }
        }

        private class NotConnectedSubscriber : ISubscriber
        {
            public readonly StartSubscriber Start;

            public NotConnectedSubscriber(StartSubscriber start)
            {
                Start = start;
            }
        }
    }
}
