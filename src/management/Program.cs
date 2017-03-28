using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using eventstore;

using EventStore.ClientAPI.Common.Log;

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

			Console.WriteLine("1 - register topics stream");
			Console.WriteLine("2 - request subscription registration");
			while (true)
	        {
		        var key = Console.ReadKey();
		        switch (key.KeyChar)
		        {
					case '1':
						EventStoreRegistry.RegisterTopicsStream(projectionManager).Wait();
						break;
					case '2':
				        eventPublisher.PublishEvent(new SubscriptionRegistrationRequested("*", "*")).Wait();
				        break;
					default:
				        return;
		        }
	        }
        }
    }
}


