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
        private readonly Action<ConnectionSettingsBuilder> _configure;

        public EventStoreConnectionFactory(Action<ConnectionSettingsBuilder> configure = null)
        {
            _configure = configure;
        }

        public IEventStoreConnection CreateConnection()
        {
            var connectionSettingsBuilder = ConnectionSettings.Create();

            _configure?.Invoke(connectionSettingsBuilder);

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
