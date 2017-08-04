using System;

using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;

namespace eventstore
{
	public class EventStoreConnectionFactory
	{
		private readonly string _clusterDns;
		private readonly int _internalHttpPort;
        private readonly string _username;
        private readonly string _password;
		private readonly Func<ConnectionSettingsBuilder, ConnectionSettingsBuilder> _configureConnection;

		public EventStoreConnectionFactory(string clusterDns, int internalHttpPort, string username, string password, Func<ConnectionSettingsBuilder, ConnectionSettingsBuilder> configureConnection = null)
		{
			_clusterDns = clusterDns;
			_internalHttpPort = internalHttpPort;
            _username = username;
            _password = password;
			_configureConnection = configureConnection;
		}

		public IEventStoreConnection CreateConnection()
		{
            var connectionSettings = (_configureConnection ?? (x => x))(
                ConnectionSettings.Create()
                .SetDefaultUserCredentials(new UserCredentials(_username, _password)));
			var clusterSettings = ClusterSettings
				.Create()
				.DiscoverClusterViaDns()
				.SetClusterDns(_clusterDns)
				.SetClusterGossipPort(_internalHttpPort)
				.SetMaxDiscoverAttempts(int.MaxValue);
			return EventStoreConnection.Create(connectionSettings, clusterSettings);
		}
	}
}
