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
		
		public EventStoreConnectionFactory(string clusterDns, int internalHttpPort, string username, string password)
		{
			_clusterDns = clusterDns;
			_internalHttpPort = internalHttpPort;
            _username = username;
            _password = password;
		}

		public IEventStoreConnection CreateConnection()
		{
			var connectionSettings = ConnectionSettings
				.Create()
				.KeepReconnecting()
                .SetDefaultUserCredentials(new UserCredentials(_username, _password))
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
