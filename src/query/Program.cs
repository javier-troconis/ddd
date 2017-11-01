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

			var applicationEventBus = 
				EventBus.CreateEventBus
				(
					createConnection,
					registry => registry
						//.RegisterVolatileSubscriber
						//(
						//	new Subscriber1()
						//)
						.RegisterCatchupSubscriber<Subscriber2>
						(
							new Subscriber2()
								.ComposeForward(CheckpointWriter<Subscriber2>.WriteCheckpoint),
							CheckpointReader<Subscriber2>.ReadCheckpoint
						)
						//.RegisterPersistentSubscriber
						//(
						//	new Subscriber3()
						//)
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
								applicationEventBus,
								eventPublisher
							)
						)
						.RegisterPersistentSubscriber
						(
							new SubscriptionStreamProvisionerController(new SubscriptionStreamProvisioner(
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
							new PersistentSubscriptionProvisionerController(new PersistentSubscriptionProvisioner(
								new PersistentSubscriptionManager(createConnection))),
							x => x.SetSubscriptionStream<IProvisionPersistentSubscriptionRequests>()
						)
				);
			infrastructureEventBus
				.StartAllSubscribers();

			while (true) { }
		}
	}
}
