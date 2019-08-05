using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using eventstore;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
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

        public async Task<ISourceBlock<ResolvedEvent>> GetEventsSourceBlock(ILogger logger, EventStoreObjectName sourceStreamName)
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
            logger.LogInformation("******** ran GetEventsSourceBlock ********");
            return bb;
        }
    }
}
