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
				EventStoreSettings.Password,
				x => x
					.WithConnectionTimeoutOf(TimeSpan.FromMinutes(1)));

			var persistentSubscriptionManager = new PersistentSubscriptionManager(connectionFactory.CreateConnection);


			var eventBus = EventBus.Start(
				connectionFactory.CreateConnection, 
				eventBusRegistry => eventBusRegistry
						.RegisterVolatileSubscriber(
							new Subscriber1()
						)
						.RegisterCatchupSubscriber<Subscriber2>(
								new Subscriber2()
									.ComposeForward(
										new Subscriber2Continuation()),
								() => Task.FromResult(default(long?))
								)
						.RegisterPersistentSubscriber(
							new Subscriber3()
							)
						.RegisterPersistentSubscriber<ISubscriptionStreamsProvisioningRequests, SubscriptionStreamsProvisioningRequestsHandler>(
							new SubscriptionStreamsProvisioningRequestsHandler(new SubscriptionStreamProvisioner(projectionManager))
							)
						.RegisterVolatileSubscriber<IPersistentSubscriptionsProvisioningRequests, PersistentSubscriptionsProvisioningRequestsHandler>(
							new PersistentSubscriptionsProvisioningRequestsHandler(new PersistentSubscriptionProvisioner(persistentSubscriptionManager))
							))
					;

			while (true) { };
		}
	}
}
