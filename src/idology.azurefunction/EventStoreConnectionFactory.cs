﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using shared;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace idology.azurefunction
{
    public interface IEventStoreConnectionFactory
    {
        Task<IEventStoreConnection> CreateEventStoreConnection(ILogger logger);
    }

    public class EventStoreConnectionFactory : IEventStoreConnectionFactory
    {
        private readonly Singleton<Task<IEventStoreConnection>> _instanceProvider = new Singleton<Task<IEventStoreConnection>>();
        private readonly Uri _eventStoreConnectionUri;
        private readonly Func<ConnectionSettingsBuilder, ConnectionSettingsBuilder> _configureConnection;

        public EventStoreConnectionFactory(Uri eventStoreConnectionUri, Func<ConnectionSettingsBuilder, ConnectionSettingsBuilder> configureConnection)
        {
            _eventStoreConnectionUri = eventStoreConnectionUri;
            _configureConnection = configureConnection;
        }

        public Task<IEventStoreConnection> CreateEventStoreConnection(ILogger logger)
        {
            return _instanceProvider.GetInstance(async () =>
            {
                var connectionSettingsBuilder = _configureConnection(ConnectionSettings.Create());
                var connectionSettings = connectionSettingsBuilder.Build();
                var connection = EventStoreConnection.Create(connectionSettings, _eventStoreConnectionUri);
                await connection.ConnectAsync();
                return connection;
            });
        }
    }
}