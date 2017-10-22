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
    public enum SubscriberStatus
    {
        Unknown,
        Connected,
        NotConnected
    }

    /*
    public sealed class EventBus
    {
        private readonly TaskQueue _queue = new TaskQueue();
        //private readonly Func<IEventStoreConnection> _createConnection;
        //private readonly IReadOnlyDictionary<string, ConnectSubscriber> _subscriberRegistry;
        private readonly IDictionary<string, ISubscriber> _subscribers;

        private EventBus
            (
                //Func<IEventStoreConnection> createConnection,
                //IReadOnlyDictionary<string, ConnectSubscriber> subscriberRegistry,
                IDictionary<string, ISubscriber> subscribers
            )
        {
            //_createConnection = createConnection;
            //_subscriberRegistry = subscriberRegistry;
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
                        ConnectedSubscriber1 connectedSubscriber;
                        if ((connectedSubscriber = _subscribers[subscriberName] as ConnectedSubscriber1) != null)
                        {
                            _subscribers[subscriberName] = connectedSubscriber.Disconnect();
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
                        NotConnectedSubscriber1 notConnectedSubscriber;
                        if ((notConnectedSubscriber = _subscribers[subscriberName] as NotConnectedSubscriber1) != null)
                        {
                            _subscribers[subscriberName] = await notConnectedSubscriber.Connect();
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
                    //createConnection,
                    //subscriberRegistry
                    //    .ToReadOnlyDictionary
                    //    (
                    //        x => x.Name,
                    //        x => x.Connect
                    //    ),
                    subscriberRegistry
                        .ToDictionary
                        (
                            x => x.Name,
                            x => (ISubscriber)new NotConnectedSubscriber1
                            (
                                () => x.Connect(createConnection)
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
            public readonly ConnectSubscriber Connect;

            public NotConnectedSubscriber(ConnectSubscriber connect)
            {
                Connect = connect;
            }
        }


        private class ConnectedSubscriber1 : ISubscriber
        {
            private readonly SubscriberConnection _connection;
            private readonly Func<Task<SubscriberConnection>> _connect;

            public ConnectedSubscriber1(Func<Task<SubscriberConnection>> connect, SubscriberConnection connection)
            {
                _connection = connection;
                _connect = connect;
            }

            public NotConnectedSubscriber1 Disconnect()
            {
                _connection.Disconnect();
                return new NotConnectedSubscriber1(_connect);
            }
        }

        private class NotConnectedSubscriber1 : ISubscriber
        {
            private readonly Func<Task<SubscriberConnection>> _connect;

            public NotConnectedSubscriber1(Func<Task<SubscriberConnection>> connect)
            {
                _connect = connect;
            }

            public async Task<ConnectedSubscriber1> Connect()
            {
                var connection = await _connect();
                return new ConnectedSubscriber1(_connect, connection);
            }
        }


    }
    */



    public sealed class EventBus
    {
        private readonly TaskQueue _queue = new TaskQueue();
        private readonly IDictionary<string, Delegate> _subscribersOperations;

        private EventBus
            (
                IDictionary<string, Delegate> subscribersOperations
            )
        {
            _subscribersOperations = subscribersOperations;
        }

        public async Task<SubscriberStatus> StopSubscriber(string subscriberName)
        {
            if (!_subscribersOperations.ContainsKey(subscriberName))
            {
                return SubscriberStatus.Unknown;
            }

            var tsc = new TaskCompletionSource<SubscriberStatus>();
            await _queue.SendToChannel
                (
                    () =>
                    {
                        Disconnect operation;
                        if ((operation = _subscribersOperations[subscriberName] as Disconnect) != null)
                        {
                            _subscribersOperations[subscriberName] = operation();
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
            return Task.WhenAll(_subscribersOperations.Select(x => StopSubscriber(x.Key)));
        }

        public async Task<SubscriberStatus> StartSubscriber(string subscriberName)
        {
            if (!_subscribersOperations.ContainsKey(subscriberName))
            {
                return SubscriberStatus.Unknown;
            }

            var tsc = new TaskCompletionSource<SubscriberStatus>();
            await _queue.SendToChannel
                (
                    async () =>
                    {
                        Connect operation;
                        if ((operation = _subscribersOperations[subscriberName] as Connect) != null)
                        {
                            _subscribersOperations[subscriberName] = await operation();
                        }
                    },
                    channelName: subscriberName,
                    taskSucceeded: x => tsc.SetResult(SubscriberStatus.Connected)
                );
            return await tsc.Task;
        }

        public Task StartAllSubscribers()
        {
            return Task.WhenAll(_subscribersOperations.Select(x => StartSubscriber(x.Key)));
        }

        public static EventBus CreateEventBus(Func<IEventStoreConnection> createConnection, Func<SubscriberRegistry, SubscriberRegistry> configureSubscriberRegistry)
        {
            var subscriberRegistry = configureSubscriberRegistry(SubscriberRegistry.CreateSubscriberRegistry());

            return new EventBus
                (
                    subscriberRegistry
                        .ToDictionary
                        (
                            x => x.Name,
                            x =>
                            {
                                Connect connect = null;
                                return (Delegate)
                                (
                                    connect = async () =>
                                    {
                                        var connection = await x.Connect(createConnection);
                                        return () =>
                                            {
                                                connection.Disconnect();
                                                return connect;
                                            };
                                    }
                                );
                            }
                        )
                );
        }

        private delegate Task<Disconnect> Connect();

        private delegate Connect Disconnect(); 
    }
}
