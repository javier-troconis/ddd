using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using shared;

namespace idology.azurefunction
{
    public interface IEventStoreConnectionProvider
    {
        Task<IEventStoreConnection> ProvideEventStoreConnection(Microsoft.Extensions.Logging.ILogger logger);
    }

    public class EventStoreConnectionProvider : IEventStoreConnectionProvider
    {
        private readonly Singleton<Task<IEventStoreConnection>> _instanceProvider = new Singleton<Task<IEventStoreConnection>>();
        private readonly Uri _uri;
        private readonly Func<ConnectionSettingsBuilder, ConnectionSettingsBuilder> _configureConnection;

        public EventStoreConnectionProvider(Uri uri, Func<ConnectionSettingsBuilder, ConnectionSettingsBuilder> configureConnection)
        {
            _uri = uri;
            _configureConnection = configureConnection;
        }

        public Task<IEventStoreConnection> ProvideEventStoreConnection(Microsoft.Extensions.Logging.ILogger logger)
        {
            return _instanceProvider.GetInstance(async () =>
            {
                var connectionSettingsBuilder = _configureConnection(ConnectionSettings.Create());
                var connectionSettings = connectionSettingsBuilder.Build();
                var connection = EventStoreConnection.Create(connectionSettings, _uri);
                await connection.ConnectAsync();
                return connection;
            });
        }
    }
}
