using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Logging;
using shared;

namespace idology.azurefunction
{
    public class GetEventStoreConnectionService
    {
        private readonly Uri _eventStoreConnectionUri;
        private readonly Func<ConnectionSettingsBuilder, ConnectionSettingsBuilder> _configureConnection;

        public GetEventStoreConnectionService(Uri eventStoreConnectionUri, Func<ConnectionSettingsBuilder, ConnectionSettingsBuilder> configureConnection)
        {
            _eventStoreConnectionUri = eventStoreConnectionUri;
            _configureConnection = configureConnection;
        }

        public async Task<IEventStoreConnection> GetEventStoreConnection(Microsoft.Extensions.Logging.ILogger logger)
        {
            var connectionSettingsBuilder = _configureConnection(ConnectionSettings.Create());
            var connectionSettings = connectionSettingsBuilder.Build();
            var connection = EventStoreConnection.Create(connectionSettings, _eventStoreConnectionUri);
            await connection.ConnectAsync();
            return connection;
        }
    }
}
