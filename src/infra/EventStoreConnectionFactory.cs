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

            //return EventStoreConnection.Create(connectionSettings, EventStoreSettings.TcpEndPoint);

            var clusterSettings = ClusterSettings
                .Create()

                .DiscoverClusterViaGossipSeeds()
                .SetGossipSeedEndPoints(
                    new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1113),
                    new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2113),
                    new IPEndPoint(IPAddress.Parse("127.0.0.1"), 3113)
                 )
                .Build();

            return EventStoreConnection.Create(connectionSettings, clusterSettings);
        }
    }
}
