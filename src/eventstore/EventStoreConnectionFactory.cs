using EventStore.ClientAPI;

namespace eventstore
{
	public class EventStoreConnectionFactory
	{
		private readonly string _clusterDns;
		private readonly int _internalHttpPort;


		
		public EventStoreConnectionFactory(string clusterDns, int internalHttpPort)
		{
			_clusterDns = clusterDns;
			_internalHttpPort = internalHttpPort;
		}

		public IEventStoreConnection CreateConnection()
		{
			var connectionSettings = ConnectionSettings
				.Create()
				.KeepReconnecting()
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
