using System;
using System.Threading.Tasks;

using eventstore;

using EventStore.ClientAPI;
using EventStore.ClientAPI.Common.Log;

using management.contracts;

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

            var subscriptionProjectionRegistry = new ProjectionRegistry(projectionManager);

            var connectionFactory = new EventStoreConnectionFactory(
                EventStoreSettings.ClusterDns,
                EventStoreSettings.InternalHttpPort,
                EventStoreSettings.Username,
                EventStoreSettings.Password);

			var persistentSubscriptionRegistry = new PersistentSubscriptionRegistry(new PersistentSubscriptionManager(connectionFactory.CreateConnection));

			var eventBus = new EventBus(connectionFactory.CreateConnection)
					.RegisterCatchupSubscriber(
							new Subscriber2(),
							() => Task.FromResult(default(long?)))
					.RegisterCatchupSubscriber(
							new Subscriber1(),
							() => Task.FromResult(default(long?)))
					.RegisterPersistentSubscriber(
							new Subscriber3())
					.RegisterPersistentSubscriber<ISubscriptionStreamRegistrationRequestedHandler>(
							new SubscriptionStreamRegistrationRequestedHandler("*", subscriptionProjectionRegistry))
					.RegisterVolatileSubscriber<IPersistentSubscriptionRegistrationRequestedHandler>(
							new PersistentSubscriptionRegistrationRequestedHandler("*", persistentSubscriptionRegistry));

			Parallel.For(1, 3, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, x => eventBus.Start());

			while (true) { };
        }

        private static readonly Func<ResolvedEvent, Task<ResolvedEvent>> _writeCheckpoint = resolvedEvent =>
		{
			Console.WriteLine("checkpointing - " + resolvedEvent.OriginalEventNumber);
			return Task.FromResult(resolvedEvent);
		};
	}
}
