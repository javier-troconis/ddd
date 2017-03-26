using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;

namespace infra
{
   // public interface IEventStoreConnectionFactory
   // {
   //     IEventStoreConnection CreateConnection();
   // }

   // public class EventStoreConnectionFactory : IEventStoreConnectionFactory
   // {
   //     private readonly Func<ConnectionSettingsBuilder, ConnectionSettingsBuilder> _configure;

   //     public EventStoreConnectionFactory(Func<ConnectionSettingsBuilder, ConnectionSettingsBuilder> configure)
   //     {
   //         _configure = configure;
   //     }

   //     public IEventStoreConnection CreateConnection()
   //     {
			//var connectionSettingsBuilder = _configure(ConnectionSettings.Create());
   //         var connectionSettings = connectionSettingsBuilder.Build();
   //         var clusterSettings = ClusterSettings
   //             .Create()
   //             .DiscoverClusterViaDns()
   //             .SetClusterDns(EventStoreSettings.ClusterDns)
   //             .SetClusterGossipPort(EventStoreSettings.InternalHttpPort)
   //             .SetMaxDiscoverAttempts(int.MaxValue)
   //             .Build();
   //         return EventStoreConnection.Create(connectionSettings, clusterSettings);
   //     }
   // }
}
