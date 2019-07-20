﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using eventstore;
using EventStore.ClientAPI;
using shared;

namespace idology.azurefunction
{
    public interface IEventReceiverFactory
    {
        Task<IEventReceiver> CreateEventReceiver(Microsoft.Extensions.Logging.ILogger logger, Predicate<ResolvedEvent> filter);
    }

    public interface IEventReceiver
    {
        Task<ResolvedEvent> Receive(CancellationToken cancellationToken);
    }

    public class EventReceiverFactory : IEventReceiverFactory
    {
        private readonly Singleton<Task<BroadcastBlock<ResolvedEvent>>> _instanceProvider = new Singleton<Task<BroadcastBlock<ResolvedEvent>>>();
        private readonly Uri _eventStoreConnectionUri;
        private readonly Func<ConnectionSettingsBuilder, ConnectionSettingsBuilder> _configureConnection;
        private readonly string _receiverName;
        private readonly string _sourceStreamName;

        public EventReceiverFactory(Uri eventStoreConnectionUri, Func<ConnectionSettingsBuilder, ConnectionSettingsBuilder> configureConnection, string receiverName, EventStoreObjectName sourceStreamName)
        {
            _eventStoreConnectionUri = eventStoreConnectionUri;
            _configureConnection = configureConnection;
            _receiverName = receiverName;
            _sourceStreamName = sourceStreamName;
        }

        // return ISourceBlock<TOutput> ?
        public async Task<IEventReceiver> CreateEventReceiver(Microsoft.Extensions.Logging.ILogger logger, Predicate<ResolvedEvent> filter)
        {
            BroadcastBlock<ResolvedEvent> bb;
            bb = await _instanceProvider.GetInstance(async () =>
            {
                bb = new BroadcastBlock<ResolvedEvent>(x => x);
                var eventBus = EventBus.CreateEventBus
                (
                    () =>
                        {
                            var connectionSettingsBuilder = _configureConnection(ConnectionSettings.Create());
                            var connectionSettings = connectionSettingsBuilder.Build();
                            var connection = EventStoreConnection.Create(connectionSettings, _eventStoreConnectionUri);
                            return connection;
                        },
                    registry => registry.RegisterVolatileSubscriber(_receiverName, _sourceStreamName, bb.SendAsync)
                );
                await eventBus.StartAllSubscribers();
                return bb;
            });
            var wob = new WriteOnceBlock<ResolvedEvent>(x => x);
            bb.LinkTo(wob, new DataflowLinkOptions { MaxMessages = 1 }, filter);
            return new EventReceiver(wob.ReceiveAsync);
        }

        private class EventReceiver : IEventReceiver
        {
            private readonly Func<CancellationToken, Task<ResolvedEvent>> _receive;

            public EventReceiver(Func<CancellationToken, Task<ResolvedEvent>> receive)
            {
                _receive = receive;
            }

            public Task<ResolvedEvent> Receive(CancellationToken cancellationToken)
            {
                return _receive(cancellationToken);
            }
        }
    }
}
