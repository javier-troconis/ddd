using System;
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
        Task<Func<CancellationToken, Task<ResolvedEvent>>> CreateEventReceiver(Microsoft.Extensions.Logging.ILogger logger, Predicate<ResolvedEvent> eventFilter);
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

        public async Task<Func<CancellationToken, Task<ResolvedEvent>>> CreateEventReceiver(Microsoft.Extensions.Logging.ILogger logger, Predicate<ResolvedEvent> eventFilter)
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
            bb.LinkTo(wob, new DataflowLinkOptions { MaxMessages = 1 }, eventFilter);
            return wob.ReceiveAsync;
        }

    
    }
}
