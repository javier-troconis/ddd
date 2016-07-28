using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;

namespace infra
{
    public interface IEventStoreConnectionFactory
    {
        IEventStoreConnection CreateConnection();
    }

    public class EventStoreConnectionFactory : IEventStoreConnectionFactory
    {
        private readonly Action<ConnectionSettingsBuilder> _configureConnectionSettings;

        public EventStoreConnectionFactory(Action<ConnectionSettingsBuilder> configureConnectionSettings = null)
        {
            _configureConnectionSettings = configureConnectionSettings;
        }

        public IEventStoreConnection CreateConnection()
        {
            var connectionSettingsBuilder = ConnectionSettings.Create();

            _configureConnectionSettings?.Invoke(connectionSettingsBuilder);

            var connectionSettings = connectionSettingsBuilder.Build();

            var clusterSettings = ClusterSettings
                .Create()
                .DiscoverClusterViaDns()
                .SetClusterDns(EventStoreSettings.ClusterDns)
                .SetClusterGossipPort(EventStoreSettings.InternalHttpPort)
                .SetMaxDiscoverAttempts(int.MaxValue)
                .Build();

            var connection = EventStoreConnection.Create(connectionSettings, clusterSettings);

            connection.Disconnected += (s, a) =>
            {
                Console.WriteLine("disconnected");
            };

            connection.Closed += (s, a) =>
            {
                Console.WriteLine("closed");
            };

            connection.ErrorOccurred += (s, a) =>
            {
                Console.WriteLine("errorocurred" + a.Exception);
            };

            connection.Connected += (s, a) =>
            {
                Console.WriteLine("connected");
            };

            connection.Reconnecting += (s, a) =>
            {
                Console.WriteLine("reconnecting");
            };

            return connection;
        }
    }
}
