 using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using eventstore;
 using EventStore.ClientAPI;
 using EventStore.ClientAPI.Common.Log;


namespace management
{
    public class Program
    {
        public static void Main(string[] args)
        {
			Func<IEventStoreConnection> createConnection = new EventStoreConnectionFactory(
                EventStoreSettings.ClusterDns,
                EventStoreSettings.InternalHttpPort,
                EventStoreSettings.Username,
                EventStoreSettings.Password, 
				x => x
					.WithConnectionTimeoutOf(TimeSpan.FromMinutes(1)))
					.CreateConnection;

			var connection = createConnection();
			connection.ConnectAsync().Wait();
            IEventPublisher eventPublisher = new EventPublisher(new eventstore.EventStore(connection));


			var applicationEventBus =
				EventBus.CreateEventBus
				(
				createConnection,
				registry => registry
					.RegisterPersistentSubscriber
					(
						new RestartSubscriberWorkflowController(eventPublisher)
					)
					.RegisterPersistentSubscriber
					(
						new ProvisionSubscriptionStreamWorkflowController(eventPublisher)
					)
				);
	        applicationEventBus
		        .StartAllSubscribers();

			var infrastructureEventBus =
				EventBus.CreateEventBus
				(
					createConnection,
					registry => registry
						.RegisterVolatileSubscriber
						(
							new EventBusController
							(
								applicationEventBus,
								eventPublisher
							)
						)
						.RegisterPersistentSubscriber
						(
							new SubscriptionStreamProvisioningController(new SubscriptionStreamProvisioningService(
								new ProjectionManager(
									EventStoreSettings.ClusterDns,
									EventStoreSettings.ExternalHttpPort,
									EventStoreSettings.Username,
									EventStoreSettings.Password,
									new ConsoleLogger()))),
							x => x.SetSubscriptionStream<ISubscriptionStreamProvisioningController>()
						)
						.RegisterVolatileSubscriber
						(
							new PersistentSubscriptionProvisioningController(new PersistentSubscriptionProvisioningService(
								new PersistentSubscriptionManager(createConnection))),
							x => x.SetSubscriptionStream<IPersistentSubscriptionProvisioningController>()
						)
				);
	        infrastructureEventBus
		        .StartAllSubscribers();


			while (true)
            {
                Console.WriteLine("1 - ProvisionSystemStreams");
				Console.WriteLine("2 - ProvisionAllPersistentSubscriptions");
				Console.WriteLine("3 - ProvisionAllSubscriptionStreams");
	            Console.WriteLine("4 - Start - query_subscriber2");
	            Console.WriteLine("5 - Stop - query_subscriber2");
				Console.WriteLine("6 - RestartSubscriber - query_subscriber2");
	            Console.WriteLine("7 - ProvisionSubscriptionStream - query_subscriber2");

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
                        var topicStreamProvisioner = new TopicStreamProvisioningService(projectionManager);
                        var subscriptionStreamProvisioner = new SubscriptionStreamProvisioningService(projectionManager);
						var systemStreamProvisioner = new SystemStreamsProvisioningService(topicStreamProvisioner, subscriptionStreamProvisioner);
						systemStreamProvisioner.ProvisionSystemStreams();
                        break;
                    case '2':
	                    eventPublisher.PublishEvent(new ProvisionAllPersistentSubscriptions());
                        break;
					case '3':
						eventPublisher.PublishEvent(new ProvisionAllSubscriptionStreams());
                        break;
	                case '4':
		                eventPublisher.PublishEvent(new StartSubscriber("query_subscriber2"));
		                break;
					case '5':
						eventPublisher.PublishEvent(new StopSubscriber("query_subscriber2"));
						break;
	                case '6':
						eventPublisher.PublishEvent(new RunRestartSubscriberWorkflow(Guid.NewGuid(), "query_subscriber2"));
						break;
	                case '7':
		                eventPublisher.PublishEvent(new RunProvisionSubscriptionStreamWorkflow(Guid.NewGuid(), "query_subscriber2", "query_subscriber2"));
		                break;
					default:
                        return;
                }
                Console.WriteLine();
	            Console.WriteLine();
			}
        }
    }
}


