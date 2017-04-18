using System;
using System.Threading.Tasks;

using eventstore;

using EventStore.ClientAPI;
using EventStore.ClientAPI.Common.Log;

using shared;

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
                EventStoreSettings.Password);

			var persistentSubscriptionManager = new PersistentSubscriptionManager(connectionFactory.CreateConnection);

			var eventBus = new EventBus(connectionFactory.CreateConnection)
					.RegisterCatchupSubscriber(
							new Subscriber2(),
							() => Task.FromResult(default(long?))
							)
					.RegisterCatchupSubscriber(
							new Subscriber1(),
							() => Task.FromResult(default(long?))
							)
					.RegisterPersistentSubscriber(
							new Subscriber3())
					.RegisterPersistentSubscriber<ISubscriptionStreamsProvisioningRequests, SubscriptionStreamsProvisioningRequestsHandler>(
							new SubscriptionStreamsProvisioningRequestsHandler(new StreamProvisioner(projectionManager))
							)
					.RegisterVolatileSubscriber<IPersistentSubscriptionsProvisioningRequests, PersistentSubscriptionsProvisioningRequestsHandler>(
							new PersistentSubscriptionsProvisioningRequestsHandler(new PersistentSubscriptionProvisioner(persistentSubscriptionManager))
							);

			Parallel.For(1, 2, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, x => eventBus.Start());


			while (true) { };
        }
	}
}
