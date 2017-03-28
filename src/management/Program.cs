using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using eventstore;

using EventStore.ClientAPI.Common.Log;

using management.contracts;

namespace management
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
			IEventPublisher eventPublisher = new EventPublisher(new eventstore.EventStore(connection));
			connection.ConnectAsync().Wait();

		
			while (true)
	        {
				Console.WriteLine("1 - register topics stream");
				Console.WriteLine("2 - register subscription registration requested handler stream");
				Console.WriteLine("3 - publish subscription registration requested");
		        var key = Console.ReadKey();
		        switch (key.KeyChar)
		        {
					case '1':
						EventStoreRegistry.RegisterTopicsStream(projectionManager).Wait();
						break;
					case '2':
						EventStoreRegistry.RegisterSubscriptionStream<ISubscriptionRegistrationRequestedHandler>(projectionManager).Wait();
						break;
					case '3':
				        eventPublisher.PublishEvent(new SubscriptionRegistrationRequested("*", "*")).Wait();
				        break;
					default:
				        return;
		        }
				Console.WriteLine();
			}
        }
    }
}


