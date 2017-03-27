using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;

namespace infra
{
	public class EventStoreConnectionFactory
	{
		private readonly string _clusterDns;
		private readonly int _internalHttpPort;
		private readonly Func<ConnectionSettingsBuilder, ConnectionSettingsBuilder> _configure;

		public EventStoreConnectionFactory(string clusterDns, int internalHttpPort, Func<ConnectionSettingsBuilder, ConnectionSettingsBuilder> configure = null)
		{
			_clusterDns = clusterDns;
			_internalHttpPort = internalHttpPort;
			_configure = configure;
		}

		public IEventStoreConnection CreateConnection()
		{
			var connectionSettings = _configure?.Invoke(ConnectionSettings.Create())
				.Build();
			var clusterSettings = ClusterSettings
				.Create()
				.DiscoverClusterViaDns()
				.SetClusterDns(_clusterDns)
				.SetClusterGossipPort(_internalHttpPort)
				.SetMaxDiscoverAttempts(int.MaxValue)
				.Build();
			return EventStoreConnection.Create(connectionSettings, clusterSettings);
		}
	}
}
