using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace idology.azurefunction
{
    public class CreateEventStoreConnectionService
    {
        private readonly Uri _eventStoreConnectionUri;
        private readonly Func<ConnectionSettingsBuilder, ConnectionSettingsBuilder> _configureConnection;

        public CreateEventStoreConnectionService(Uri eventStoreConnectionUri, Func<ConnectionSettingsBuilder, ConnectionSettingsBuilder> configureConnection)
        {
            _eventStoreConnectionUri = eventStoreConnectionUri;
            _configureConnection = configureConnection;
        }

        public async Task<IEventStoreConnection> CreateEventStoreConnection(ILogger logger)
        {
            var connectionSettingsBuilder = _configureConnection(ConnectionSettings.Create());
            var connectionSettings = connectionSettingsBuilder.Build();
            var connection = EventStoreConnection.Create(connectionSettings, _eventStoreConnectionUri);
            await connection.ConnectAsync();
            return connection;
        }
    }
}
