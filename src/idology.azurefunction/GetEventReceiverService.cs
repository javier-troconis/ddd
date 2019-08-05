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
    public class GetEventsSourceBlockService
    {
        private readonly Uri _eventStoreConnectionUri;
        private readonly Func<ConnectionSettingsBuilder, ConnectionSettingsBuilder> _configureConnection;

        public GetEventsSourceBlockService(Uri eventStoreConnectionUri, Func<ConnectionSettingsBuilder, ConnectionSettingsBuilder> configureConnection)
        {
            _eventStoreConnectionUri = eventStoreConnectionUri;
            _configureConnection = configureConnection;
        }

        public async Task<ISourceBlock<ResolvedEvent>> GetEventsSourceBlock(EventStoreObjectName sourceStreamName, ILogger logger)
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
    }

    public class GetEventSourceBlockService
    {
        private readonly Func<EventStoreObjectName, ILogger, Task<ISourceBlock<ResolvedEvent>>> _getEventsSourceBlock;

        public GetEventSourceBlockService(Func<EventStoreObjectName, ILogger, Task<ISourceBlock<ResolvedEvent>>> getEventsSourceBlock)
        {
            _getEventsSourceBlock = getEventsSourceBlock;
        }

        public async Task<ISourceBlock<ResolvedEvent>> GetEventSourceBlock(EventStoreObjectName sourceStreamName, ILogger logger, Predicate<ResolvedEvent> eventFilter)
        {
            var eventSourceBlock = await _getEventsSourceBlock(sourceStreamName, logger);
            var wob = new WriteOnceBlock<ResolvedEvent>(x => x);
            eventSourceBlock.LinkTo(wob, new DataflowLinkOptions { MaxMessages = 1 }, eventFilter);
            return wob;
        }
    }
}
