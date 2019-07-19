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
    public delegate Task<ResolvedEvent> StartEventSubscriber(CancellationToken cancellationToken);

    public interface IEventReceiverFactoryProvider
    {
        Task<IEventReceiver> CreateEventReceiver(Predicate<ResolvedEvent> eventFilter, Microsoft.Extensions.Logging.ILogger logger);
    }

    public interface IEventReceiver
    {
        Task<ResolvedEvent> Receive(CancellationToken cancellationToken);
    }

    public class EventReceiverFactoryProvider : IEventReceiverFactoryProvider
    {
        private readonly Singleton<Task<BroadcastBlock<ResolvedEvent>>> _instanceProvider = new Singleton<Task<BroadcastBlock<ResolvedEvent>>>();
        private readonly Func<ConnectionSettingsBuilder, ConnectionSettingsBuilder> _configureConnection;

        public EventReceiverFactoryProvider(Func<ConnectionSettingsBuilder, ConnectionSettingsBuilder> configureConnection)
        {
            _configureConnection = configureConnection;
        }

        public async Task<IEventReceiver> CreateEventReceiver(Predicate<ResolvedEvent> filter, Microsoft.Extensions.Logging.ILogger logger)
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
                            var connection = EventStoreConnection.Create(connectionSettings);
                            return connection;
                        },
                    registry => registry.RegisterVolatileSubscriber("subscriberName", "subscriptionStreamName", bb.SendAsync)
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
