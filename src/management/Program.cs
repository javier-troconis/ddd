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


	        var consumerEventBus =
		        EventBus.CreateEventBus
		        (
			        createConnection,
			        registry => registry
				        .RegisterPersistentSubscriber
				        (
					        new RestartSubscriberWorkflow(new eventstore.EventStore(connection))
				        )
		        );


			var infrastructureEventBus =
		        EventBus.CreateEventBus
		        (
			        createConnection,
			        registry => registry
				        .RegisterVolatileSubscriber
				        (
					        new EventBusController
					        (
						        consumerEventBus,
						        eventPublisher
					        )
				        )
				        .RegisterPersistentSubscriber
				        (
					        new ProvisionSubscriptionStream(new SubscriptionStreamProvisioner(
						        new ProjectionManager(
							        EventStoreSettings.ClusterDns,
							        EventStoreSettings.ExternalHttpPort,
							        EventStoreSettings.Username,
							        EventStoreSettings.Password,
							        new ConsoleLogger()))),
					        x => x.SetSubscriptionStream<IProvisionSubscriptionStreamRequests>()
				        )
				        .RegisterVolatileSubscriber
				        (
					        new ProvisionPersistentSubscription(new PersistentSubscriptionProvisioner(
						        new PersistentSubscriptionManager(createConnection))),
					        x => x.SetSubscriptionStream<IProvisionPersistentSubscriptionRequests>()
				        )
		        );
	        infrastructureEventBus
		        .StartAllSubscribers()
		        .Wait();


			while (true)
            {
                Console.WriteLine("1 - provision system streams");
				Console.WriteLine("2 - provision persistent subscriptions");
				Console.WriteLine("3 - provision subscription streams");
	            Console.WriteLine("4 - start subscriber query_subscriber3");
	            Console.WriteLine("5 - stop subscriber query_subscriber3");
				Console.WriteLine("6 - restart query_subscriber3");

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
						var systemStreamProvisioner = new SystemStreamsProvisioner(topicStreamProvisioner, subscriptionStreamProvisioner);
						systemStreamProvisioner.ProvisionSystemStreams();
                        break;
                    case '2':
						var persistentSubscriptionProvisioningRequestor = new ProvisionPersistentSubscriptionRequestor(eventPublisher);
						persistentSubscriptionProvisioningRequestor.RequestPersistentSubscriptionProvision("*");
                        break;
					case '3':
						var subscriptionStreamProvisioningRequestor = new ProvisionProvisionSubscriptionStreamRequestor(eventPublisher);
						subscriptionStreamProvisioningRequestor.RequestSubscriptionStreamProvision("*");
                        break;
	                case '4':
		                eventPublisher.PublishEvent(new StartSubscription("query_subscriber3"));
		                break;
					case '5':
						eventPublisher.PublishEvent(new StopSubscription("query_subscriber3"));
						break;
	                case '6':
		                var workflowId = Guid.NewGuid();
		                IEventStore eventStore = new eventstore.EventStore(connection);
		                eventStore.WriteEvents(
							"restart_subscriber_workflow_" + workflowId.ToString("N"),
			                ExpectedVersion.NoStream, 
							new object[]
							{
								new RestartSubscriberWorflowStarted()
							}).Wait();
						eventPublisher.PublishEvent(new StopSubscription("query_subscriber3"), x => x.SetEventId(workflowId));
						break;
					default:
                        return;
                }
                Console.WriteLine();
            }
        }
    }
}


