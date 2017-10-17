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

			var consumerEventBus = EventBus.Start(
				createConnection,
				registry => registry
						.RegisterVolatileSubscriber(
							new Subscriber1()
						)
						.RegisterCatchupSubscriber<Subscriber2>(
							new Subscriber2()
								.ComposeForward(new Subscriber2Continuation())
									.ComposeForward(CheckpointWriter<Subscriber2>.WriteCheckpoint),
							CheckpointReader<Subscriber2>.ReadCheckpoint
							)
						.RegisterPersistentSubscriber(
							new Subscriber3()
						))
					;

			var infrastructureEventBus = EventBus.Start(
					createConnection,
					registry => registry
						//.RegisterVolatileSubscriber(
						//	new EventBusController
						//	(
						//		createConnection,
						//		SubscriberRegistry
						//			.CreateSubscriberRegistry()
						//				.RegisterVolatileSubscriber(
						//					new Subscriber1()
						//				)
						//			.RegisterCatchupSubscriber<Subscriber2>(
						//				new Subscriber2()
						//					.ComposeForward(new Subscriber2Continuation())
						//						.ComposeForward(CheckpointWriter<Subscriber2>.WriteCheckpoint),
						//				CheckpointReader<Subscriber2>.ReadCheckpoint
						//			)
						//			.RegisterPersistentSubscriber(
						//				new Subscriber3()
						//			)
						//	))
						.RegisterPersistentSubscriber<IProvisionSubscriptionStreamRequests, ProvisionSubscriptionStream>(
							new ProvisionSubscriptionStream(new SubscriptionStreamProvisioner(
								new ProjectionManager(
									EventStoreSettings.ClusterDns,
									EventStoreSettings.ExternalHttpPort,
									EventStoreSettings.Username,
									EventStoreSettings.Password,
									new ConsoleLogger())))
						)
						.RegisterVolatileSubscriber<IProvisionPersistentSubscriptionRequests, ProvisionPersistentSubscription>(
							new ProvisionPersistentSubscription(new PersistentSubscriptionProvisioner(
								new PersistentSubscriptionManager(createConnection)))
						))
				;

			while (true) { };
		}
	}
}
