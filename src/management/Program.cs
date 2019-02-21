 using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using eventstore;
 using EventStore.ClientAPI;
 using EventStore.ClientAPI.Common.Log;
 using EventStore.ClientAPI.SystemData;


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
				z =>
					z.RegisterPersistentSubscriber
					(
						new RestartSubscriberScriptController(new ScriptEvaluationService(eventPublisher), new RestartSubscriberScriptDefinition())
					)
				);
			consumerEventBus
				.StartAllSubscribers();

			var infrastructureEventBus =
				EventBus.CreateEventBus
				(
					createConnection,
					z => z
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
							new SubscriptionStreamProvisionerController(new SubscriptionStreamProvisioner(
								new ProjectionManager(
									EventStoreSettings.ClusterDns,
									EventStoreSettings.ExternalHttpPort,
									new ConsoleLogger()))),
							x => x.SetSubscriptionStreamName(typeof(IProvisionSubscriptionStreamRequests))
						)
						.RegisterVolatileSubscriber
						(
							new PersistentSubscriptionProvisionerController(new PersistentSubscriptionProvisioner(
								new PersistentSubscriptionManager(createConnection))),
							x => x.SetSubscriptionStreamName(typeof(IProvisionPersistentSubscriptionRequests))
						)
				);
	        infrastructureEventBus
		        .StartAllSubscribers();


			while (true)
            {
                Console.WriteLine("1 - provision system streams");
				Console.WriteLine("2 - provision persistent subscriptions");
				Console.WriteLine("3 - provision subscription streams");
	            Console.WriteLine("4 - start query_subscriber3");
	            Console.WriteLine("5 - stop query_subscriber3");
				Console.WriteLine("6 - restart(stop -> start) query_subscriber3");

				var option = Console.ReadKey().KeyChar;
                switch (option)
                {
                    case '1':
						var projectionManager = new ProjectionManager(
							EventStoreSettings.ClusterDns,
							EventStoreSettings.ExternalHttpPort,
							new ConsoleLogger());
                        var topicStreamProvisioner = new TopicStreamProvisioner(projectionManager);
                        var subscriptionStreamProvisioner = new SubscriptionStreamProvisioner(projectionManager);
						var systemStreamProvisioner = new SystemStreamsProvisioner(topicStreamProvisioner, subscriptionStreamProvisioner);
						systemStreamProvisioner.ProvisionSystemStreams(new UserCredentials(EventStoreSettings.Username, EventStoreSettings.Password));
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
						eventPublisher.PublishEvent(new StartSubscriber("query_Subscriber3"));
						break;
					case '5':
						eventPublisher.PublishEvent(new StopSubscriber("query_Subscriber3"));
						break;
					case '6':
						var scriptInstanceId = Guid.NewGuid();
						var scriptData =
							new RestartSubscriberScriptData
							{
								SubscriberName = "query_Subscriber3"
							};
						IScriptEvaluationService scriptEvaluationService = new ScriptEvaluationService(eventPublisher);
						scriptEvaluationService.StartScript(scriptInstanceId, new RestartSubscriberScriptDefinition(), ScriptFlowType.AscendingSequence, scriptData);
						break;
					default:
                        return;
                }
                Console.WriteLine();
			}
        }
    }
}


