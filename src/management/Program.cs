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
            IEventPublisher eventPublisher = new EventPublisher(new eventstore.EventStore(connection));
            connection.ConnectAsync();

            while (true)
            {
                Console.WriteLine("1 - create topics projection");
                Console.WriteLine("2 - create setup persistent subscriptions command projection");
				Console.WriteLine("3 - create setup subscription projections command projection");
				Console.WriteLine("4 - send setup persistent subscriptions command");
				Console.WriteLine("5 - send setup subscription projections command");
                switch (Console.ReadKey().KeyChar)
                {
                    case '1':
						topicsProjectionRegistry.RegisterTopicsProjection();
                        break;
                    case '2':
						subscriptionProjectionRegistry.RegisterSubscriptionProjection<IRegisterPersistentSubscriptionHandler>();
                        break;
					case '3':
						subscriptionProjectionRegistry.RegisterSubscriptionProjection<IRegisterSubscriptionProjectionHandler>();
						break;
					case '4':
                        eventPublisher.PublishEvent(new RegisterPersistentSubscription("*", "*"));
                        break;
					case '5':
						eventPublisher.PublishEvent(new RegisterSubscriptionProjection("*", "*"));
						break;
					default:
                        return;
                }
                Console.WriteLine();
            }
        }
    }
}


