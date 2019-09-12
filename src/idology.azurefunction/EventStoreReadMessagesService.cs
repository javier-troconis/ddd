using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using eventstore;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace idology.azurefunction
{
    public class EventStoreReadMessagesService : IReadMessagesService<ResolvedEvent>
    {
        private readonly Func<ILogger, Task<IEventStoreConnection>> _createEventStoreConnection;

        public EventStoreReadMessagesService(Func<ILogger, Task<IEventStoreConnection>> createEventStoreConnection)
        {
            _createEventStoreConnection = createEventStoreConnection;
        }

        public async Task<ResolvedEvent[]> ReadMessages(string streamName, ILogger logger)
        {
            var connection = await _createEventStoreConnection(logger);
            var eventsSlice = await connection
                .ReadStreamEventsForwardAsync(streamName, 0, 4096, true,
                    new UserCredentials(EventStoreSettings.Username, EventStoreSettings.Password));
            return eventsSlice.Status == SliceReadStatus.Success ? eventsSlice.Events : null;
        }
    }
}
