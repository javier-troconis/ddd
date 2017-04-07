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

            var connectionFactory = new EventStoreConnectionFactory(
                EventStoreSettings.ClusterDns,
                EventStoreSettings.InternalHttpPort,
                EventStoreSettings.Username,
                EventStoreSettings.Password);

			ITopicsProjectionRegistry topicsProjectionRegistry = new ProjectionRegistry(projectionManager);
			ISubscriptionProjectionRegistry subscriptionProjectionRegistry = new ProjectionRegistry(projectionManager);

			var connection = connectionFactory.CreateConnection();
			connection.ConnectAsync().Wait();
            IEventPublisher eventPublisher = new EventPublisher(new eventstore.EventStore(connection));
            

            while (true)
            {
                Console.WriteLine("1 - register system streams");
				Console.WriteLine("2 - request persistent subscription registrations");
				Console.WriteLine("3 - request subscription stream registrations");
                switch (Console.ReadKey().KeyChar)
                {
                    case '1':
						topicsProjectionRegistry.RegisterTopicsProjection();
                        subscriptionProjectionRegistry.RegisterSubscriptionProjection<IPersistentSubscriptionRegistrationRequestedHandler>();
                        subscriptionProjectionRegistry.RegisterSubscriptionProjection<ISubscriptionStreamRegistrationRequested>();
                        break;
					case '2':
                        eventPublisher.PublishEvent(new PersistentSubscriptionRegistrationRequested("*", "*"));
                        break;
					case '3':
						eventPublisher.PublishEvent(new SubscriptionStreamRegistrationRequested("*", "*"));
						break;
					default:
                        return;
                }
                Console.WriteLine();
            }
        }
    }
}


