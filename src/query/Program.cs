using System;
using System.Threading.Tasks;

using eventstore;

using EventStore.ClientAPI;
using EventStore.ClientAPI.Common.Log;

using shared;
using command.contracts;

namespace query
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
				x => x.WithConnectionTimeoutOf(TimeSpan.FromMinutes(1)))
					.CreateConnection;

			var connection = createConnection();
			connection.ConnectAsync().Wait();
			IEventPublisher eventPublisher = new EventPublisher(new eventstore.EventStore(connection));

			var consumerEventBus = 
				EventBus.CreateEventBus
				(
					createConnection,
					z => z
						.RegisterVolatileSubscriber
						(
							new Subscriber2()
						)
						.RegisterCatchupSubscriber
						(
							new Subscriber3(),
							CheckpointReader<Subscriber3>.ReadCheckpoint,
							setRegistrationOptions : y => y.SetEventHandlingProcessor(m => m.ComposeForward(CheckpointWriter<Subscriber3>.WriteCheckpoint))
						)
						//.RegisterPersistentSubscriber
						//(
						//	new Subscriber3()
						//)
				);

			consumerEventBus
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

			while (true) { }
		}
	}
}
