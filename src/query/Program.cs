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

            ISubscriptionProjectionRegistry subscriptionProjectionRegistry = new ProjectionRegistry(projectionManager);

            var connectionFactory = new EventStoreConnectionFactory(
                EventStoreSettings.ClusterDns,
                EventStoreSettings.InternalHttpPort,
                EventStoreSettings.Username,
                EventStoreSettings.Password);

            var persistentSubscriptionManager = new PersistentSubscriptionManager(connectionFactory.CreateConnection);
            var persistentSubscriptionRegistry = new PersistentSubscriptionRegistry(persistentSubscriptionManager);


            Parallel.For(1, 3, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, instanceId =>
                  new EventBus(connectionFactory.CreateConnection)
                    .RegisterCatchupSubscriber(
                        new Subscriber2(),
                            () => Task.FromResult(default(long?)),
                            _printMessage.Partial("subscriber 2 instance " + instanceId).ToAsyncInput().ComposeBackward(_writeCheckpoint).ToAsyncInput().ComposeBackward)
                    .RegisterCatchupSubscriber(
                        new Subscriber1(),
                            () => Task.FromResult(default(long?)),
                            _printMessage.Partial("subscriber 1 instance " + instanceId).ToAsyncInput().ComposeBackward(_writeCheckpoint).ToAsyncInput().ComposeBackward)
                    .RegisterPersistentSubscriber(new Subscriber3(),
                            _printMessage.Partial("subscriber 3 instance " + instanceId).ToAsyncInput().ComposeBackward)
                    .RegisterPersistentSubscriber<IProjectionsRequestedHandler>(new ProjectionsRequestedHandler("*", subscriptionProjectionRegistry))
                    .RegisterVolatileSubscriber<IPersistentSubscriptionsRequestedHandler>(new PersistentSubscriptionsRequestedHandler("*", persistentSubscriptionRegistry))
                    .Start()
            );

            while (true)
            {

            }
        }

        private static readonly Func<string, ResolvedEvent, Task<ResolvedEvent>> _printMessage = (message, resolvedEvent) =>
        {
            Console.WriteLine(message);
            return Task.FromResult(resolvedEvent);
        };


        private static readonly Func<ResolvedEvent, Task<ResolvedEvent>> _writeCheckpoint = resolvedEvent =>
		{
			Console.WriteLine("checkpointing - " + resolvedEvent.OriginalEventNumber);
			return Task.FromResult(resolvedEvent);
		};
	}
}
