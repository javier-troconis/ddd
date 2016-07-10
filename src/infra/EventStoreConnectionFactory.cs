using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;

namespace infra
{
    public static class EventStoreConnectionFactory
    {
		public static IEventStoreConnection Create(Action<ConnectionSettingsBuilder> configureConnectionSettings = null)
		{
			var connectionSettingsBuilder = ConnectionSettings
					.Create()
					.SetDefaultUserCredentials(EventStoreSettings.Credentials);

            configureConnectionSettings?.Invoke(connectionSettingsBuilder);

            var connectionSettings = connectionSettingsBuilder.Build();

            var clusterSettings = ClusterSettings
                .Create()
                .DiscoverClusterViaDns().SetClusterDns(EventStoreSettings.ClusterDns)
                .SetClusterGossipPort(EventStoreSettings.InternalHttpPort)
                .Build();

            return EventStoreConnection.Create(connectionSettings, clusterSettings);
        }
    }
}
