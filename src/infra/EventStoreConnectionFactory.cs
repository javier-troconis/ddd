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

            return EventStoreConnection.Create(connectionSettings, clusterSettings);
        }
    }
}
