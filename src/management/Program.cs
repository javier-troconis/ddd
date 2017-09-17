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
			var connectionFactory = new EventStoreConnectionFactory(
                EventStoreSettings.ClusterDns,
                EventStoreSettings.InternalHttpPort,
                EventStoreSettings.Username,
                EventStoreSettings.Password, 
				x => x
					.WithConnectionTimeoutOf(TimeSpan.FromMinutes(1)));

			var connection = connectionFactory.CreateConnection();
			connection.ConnectAsync().Wait();
            IEventPublisher eventPublisher = new EventPublisher(new eventstore.EventStore(connection));

			while (true)
            {
                Console.WriteLine("1 - provision system streams");
				Console.WriteLine("2 - request persistent subscriptions provisioning");
				Console.WriteLine("3 - request subscription streams provisioning");

                var option = Console.ReadKey().KeyChar;
                switch (option)
                {
                    case '1':
						var projectionManager = new ProjectionManager(
							EventStoreSettings.ClusterDns,
							EventStoreSettings.ExternalHttpPort,
							EventStoreSettings.Username,
							EventStoreSettings.Password,
							new ConsoleLogger());
                        var topicStreamProvisioner = new TopicStreamProvisioner(projectionManager);
                        var subscriptionStreamProvisioner = new SubscriptionStreamProvisioner(projectionManager);
						var streamProvisioner = new SystemStreamsProvisioner(topicStreamProvisioner, subscriptionStreamProvisioner);
						streamProvisioner.ProvisionSystemStreams();
                        break;
                    case '2':
						var persistentSubscriptionProvisioningRequestor = new PersistentSubscriptionProvisioningRequestor(eventPublisher);
						persistentSubscriptionProvisioningRequestor.RequestPersistentSubscriptionsProvisioning("*");
                        break;
					case '3':
						var subscriptionStreamProvisioningRequestor = new SubscriptionStreamProvisioningRequestor(eventPublisher);
						subscriptionStreamProvisioningRequestor.RequestSubscriptionStreamsProvisioning("*");
                        break;				
					default:
                        return;
                }
                Console.WriteLine();
            }
        }
    }
}


