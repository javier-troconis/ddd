using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using EventStore.ClientAPI;
using EventStore.ClientAPI.Common.Log;

using infra;

using subscriber.contracts;

using EventStore = infra.EventStore;

namespace registry
{
    public class Program
    {
        public static void Main(string[] args)
        {
	        var projectionManager = new ProjectionManager(
		        EventStoreSettings.ClusterDns,
			        EventStoreSettings.ExternalHttpPort,
			        EventStoreSettings.Username,
			        EventStoreSettings.Password,
			        new ConsoleLogger());
			var connectionFactory = new EventStoreConnectionFactory(EventStoreSettings.ClusterDns, EventStoreSettings.InternalHttpPort);
	        var connection = connectionFactory.CreateConnection();
	        connection.ConnectAsync().Wait();
			var persistentSubscriptionManager = new PersistentSubscriptionManager(connectionFactory.CreateConnection, EventStoreSettings.Username, EventStoreSettings.Password);


			EventStoreRegistry.RegisterTopicsStream(projectionManager).Wait();
	        EventStoreRegistry.RegisterPersistentSubscription<IRegisterSubscriptionsHandler>(projectionManager, persistentSubscriptionManager).Wait();
	        new infra.EventStore(connection).WriteEvents(typeof(ISubscriptionsRegistrationRequested).GetEventStoreName(), ExpectedVersion.Any, new []{new SubscriptionsRegistrationRequested()}).Wait();
        }
    }
}
