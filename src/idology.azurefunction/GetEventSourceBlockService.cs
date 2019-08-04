using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using eventstore;
using EventStore.ClientAPI;
using shared;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace idology.azurefunction
{
    public class GetEventSourceBlockService1
    {
        private readonly Uri _eventStoreConnectionUri;
        private readonly Func<ConnectionSettingsBuilder, ConnectionSettingsBuilder> _configureConnection;

        public GetEventSourceBlockService1(Uri eventStoreConnectionUri, Func<ConnectionSettingsBuilder, ConnectionSettingsBuilder> configureConnection)
        {
            _eventStoreConnectionUri = eventStoreConnectionUri;
            _configureConnection = configureConnection;
        }

        public async Task<ISourceBlock<ResolvedEvent>> GetEventSourceBlock(ILogger logger, EventStoreObjectName sourceStreamName)
        {
            var bb = new BroadcastBlock<ResolvedEvent>(x => x);
            var eventBus = EventBus.CreateEventBus
            (
                () =>
                    {
                        var connectionSettingsBuilder = _configureConnection(ConnectionSettings.Create());
                        var connectionSettings = connectionSettingsBuilder.Build();
                        var connection = EventStoreConnection.Create(connectionSettings, _eventStoreConnectionUri);
                        return connection;
                    },
                registry => registry.RegisterVolatileSubscriber(sourceStreamName, sourceStreamName, bb.SendAsync)
            );
            await eventBus.StartAllSubscribers();
            return bb;
            //return eventFilter =>
            //{
            //    var wob = new WriteOnceBlock<ResolvedEvent>(x => x);
            //    bb.LinkTo(wob, new DataflowLinkOptions {MaxMessages = 1}, eventFilter);
            //    return wob.ReceiveAsync;
            //};
        }

    
    }

    public class GetEventReceiverFactoryService
    {
        private readonly Uri _eventStoreConnectionUri;
        private readonly Func<ConnectionSettingsBuilder, ConnectionSettingsBuilder> _configureConnection;

        public GetEventReceiverFactoryService(Uri eventStoreConnectionUri, Func<ConnectionSettingsBuilder, ConnectionSettingsBuilder> configureConnection)
        {
            _eventStoreConnectionUri = eventStoreConnectionUri;
            _configureConnection = configureConnection;
        }

        public Func<Predicate<ResolvedEvent>, Task<ISourceBlock<ResolvedEvent>>> GetEventReceiverFactory(ILogger logger, EventStoreObjectName sourceStreamName)
        {
            var bb = new BroadcastBlock<ResolvedEvent>(x => x);
            var eventBus = EventBus.CreateEventBus
            (
                () =>
                {
                    var connectionSettingsBuilder = _configureConnection(ConnectionSettings.Create());
                    var connectionSettings = connectionSettingsBuilder.Build();
                    var connection = EventStoreConnection.Create(connectionSettings, _eventStoreConnectionUri);
                    return connection;
                },
                registry => registry.RegisterVolatileSubscriber(sourceStreamName, sourceStreamName, bb.SendAsync)
            );
            var startAllSubscribersTask = eventBus.StartAllSubscribers();
            return async eventFilter =>
            {
                await startAllSubscribersTask;
                var wob = new WriteOnceBlock<ResolvedEvent>(x => x);
                bb.LinkTo(wob, new DataflowLinkOptions { MaxMessages = 1 }, eventFilter);
                return wob;
            };
        }


        public async Task<ISourceBlock<ResolvedEvent>> GetStreamSourceBlock(ILogger logger, EventStoreObjectName sourceStreamName)
        {
            var bb = new BroadcastBlock<ResolvedEvent>(x => x);
            var eventBus = EventBus.CreateEventBus
            (
                () =>
                {
                    var connectionSettingsBuilder = _configureConnection(ConnectionSettings.Create());
                    var connectionSettings = connectionSettingsBuilder.Build();
                    var connection = EventStoreConnection.Create(connectionSettings, _eventStoreConnectionUri);
                    return connection;
                },
                registry => registry.RegisterVolatileSubscriber(sourceStreamName, sourceStreamName, bb.SendAsync)
            );
            await eventBus.StartAllSubscribers();
            return bb;
        }

        public ISourceBlock<ResolvedEvent> GetEventSourceBlock(ISourceBlock<ResolvedEvent> streamSourceBlock, Predicate<ResolvedEvent> eventFilter)
        {
            var wob = new WriteOnceBlock<ResolvedEvent>(x => x);
            streamSourceBlock.LinkTo(wob, new DataflowLinkOptions { MaxMessages = 1 }, eventFilter);
            return wob;
        }
    }
}
