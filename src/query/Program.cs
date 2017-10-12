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

			var eventBus1 = EventBus.Start(
				createConnection, 
				registry => registry
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
						))
					;

			var eventBus2 = EventBus.Start(
					createConnection,
					registry => registry
						.RegisterPersistentSubscriber<ISubscriptionStreamsProvisioningRequests, SubscriptionStreamsProvisioningRequestsHandler>(
							new SubscriptionStreamsProvisioningRequestsHandler(new SubscriptionStreamProvisioner(
								new ProjectionManager(
									EventStoreSettings.ClusterDns,
									EventStoreSettings.ExternalHttpPort,
									EventStoreSettings.Username,
									EventStoreSettings.Password,
									new ConsoleLogger())))
						)
						.RegisterVolatileSubscriber<IPersistentSubscriptionsProvisioningRequests, PersistentSubscriptionsProvisioningRequestsHandler>(
							new PersistentSubscriptionsProvisioningRequestsHandler(new PersistentSubscriptionProvisioner(
								new PersistentSubscriptionManager(createConnection)))
						))
				;

			while (true) { };
		}
	}
}
