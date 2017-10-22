﻿using System;
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
	public enum SubscriberStatus
	{
        Unknown,
		Connected,
		NotConnected
	}

    public sealed class EventBus
    {
        private readonly TaskQueue _queue = new TaskQueue();
        private readonly Func<IEventStoreConnection> _createConnection;
        private readonly IReadOnlyDictionary<string, ConnectSubscriber> _subscriberRegistry;
        private readonly IDictionary<string, ISubscriber> _subscribers;

        private EventBus
            (
                Func<IEventStoreConnection> createConnection,
                IReadOnlyDictionary<string, ConnectSubscriber> subscriberRegistry,
                IDictionary<string, ISubscriber> subscribers
            )
        {
            _createConnection = createConnection;
            _subscriberRegistry = subscriberRegistry;
            _subscribers = subscribers;
        }

        public async Task<SubscriberStatus> StopSubscriber(string subscriberName)
        {
            if (!_subscribers.ContainsKey(subscriberName))
            {
                return SubscriberStatus.Unknown;
            }

            var tsc = new TaskCompletionSource<SubscriberStatus>();
            await _queue.SendToChannel
                (
                    () =>
                    {
                        ConnectedSubscriber connectedSubscriber;
                        if ((connectedSubscriber = _subscribers[subscriberName] as ConnectedSubscriber) != null)
                        {
                            connectedSubscriber.Disconnect();
                            _subscribers[subscriberName] = new NotConnectedSubscriber(_subscriberRegistry[subscriberName]);
                        }
                        return Task.CompletedTask;
                    },
                    channelName: subscriberName,
                    taskSucceeded: x => tsc.SetResult(SubscriberStatus.NotConnected)
                );
            return await tsc.Task;
        }

        public Task StopAllSubscribers()
        {
            return Task.WhenAll(_subscribers.Select(x => StopSubscriber(x.Key)));
        }

        public async Task<SubscriberStatus> StartSubscriber(string subscriberName)
        {
            if (!_subscribers.ContainsKey(subscriberName))
            {
                return SubscriberStatus.Unknown;
            }

            var tsc = new TaskCompletionSource<SubscriberStatus>();
            await _queue.SendToChannel
                (
                    async () =>
                    {
                        NotConnectedSubscriber notConnectedSubscriber;
                        if ((notConnectedSubscriber = _subscribers[subscriberName] as NotConnectedSubscriber) != null)
                        {
                            _subscribers[subscriberName] = await notConnectedSubscriber.Connect(_createConnection);
                        }
                    },
                    channelName: subscriberName,
                    taskSucceeded: x => tsc.SetResult(SubscriberStatus.Connected)
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
                            x => x.Name,
                            x => x.Connect
                        ),
                    subscriberRegistry
                        .ToDictionary
                        (
                            x => x.Name,
                            x => (ISubscriber)new NotConnectedSubscriber
                            (
                                x.Connect
                            )
                        )
                );
        }

        private interface ISubscriber
        {

        }

        private class ConnectedSubscriber : ISubscriber
        {
            private readonly SubscriberConnection _connection;

            public ConnectedSubscriber(SubscriberConnection connection)
            {
                _connection = connection;
            }

            public void Disconnect()
            {
                _connection.Disconnect();
            }
        }

        private class NotConnectedSubscriber : ISubscriber
        {
            private readonly ConnectSubscriber _connect;

            public NotConnectedSubscriber(ConnectSubscriber connect)
            {
                _connect = connect;
            }

            public async Task<ConnectedSubscriber> Connect(Func<IEventStoreConnection> createConnection)
            {
                var connection = await _connect(createConnection);
                return new ConnectedSubscriber(connection);
            }
        }
    }
}
