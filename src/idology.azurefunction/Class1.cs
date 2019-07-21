using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using eventstore;
using EventStore.ClientAPI;
using Microsoft.Extensions.Caching.Memory;
using shared;

namespace idology.azurefunction
{
    public delegate Task<ISourceBlock<ResolvedEvent>> CreateEventsProducer(ILogger logger);
    public delegate ISourceBlock<ResolvedEvent> CreateEventProducer(ISourceBlock<ResolvedEvent> eventsProducer, Predicate<ResolvedEvent> eventFilter);

    public static class Class1
    {
        public class StreamSubscriberConfiguration
        {
            public readonly Uri EventStoreConnectionUri;
            public readonly Func<ConnectionSettingsBuilder, ConnectionSettingsBuilder> ConfigureConnection;
            public readonly string SubscriberName;
            public readonly string SubscriptionStreamName;

            public StreamSubscriberConfiguration(Uri eventStoreConnectionUri, Func<ConnectionSettingsBuilder, ConnectionSettingsBuilder> configureConnection, string subscriberName, EventStoreObjectName subscriptionStreamName)
            {
                EventStoreConnectionUri = eventStoreConnectionUri;
                ConfigureConnection = configureConnection;
                SubscriberName = subscriberName;
                SubscriptionStreamName = subscriptionStreamName;
            }
        }

        public static Func<StreamSubscriberConfiguration, ILogger, Task<ISourceBlock<ResolvedEvent>>> CreateEventsProducer =
            async (streamSubscriberConfiguration, logger) =>
            {
                var bb = new BroadcastBlock<ResolvedEvent>(x => x);
                var eventBus = EventBus.CreateEventBus
                (
                    () =>
                    {
                        var connectionSettingsBuilder = streamSubscriberConfiguration.ConfigureConnection(ConnectionSettings.Create());
                        var connectionSettings = connectionSettingsBuilder.Build();
                        var connection = EventStoreConnection.Create(connectionSettings, streamSubscriberConfiguration.EventStoreConnectionUri);
                        return connection;
                    },
                    registry => registry.RegisterVolatileSubscriber(streamSubscriberConfiguration.SubscriberName, streamSubscriberConfiguration.SubscriptionStreamName, bb.SendAsync)
                );
                await eventBus.StartAllSubscribers();
                return bb;
            };

        public static Func<ISourceBlock<ResolvedEvent>, Predicate<ResolvedEvent>, ISourceBlock<ResolvedEvent>> CreateOneTimeEventProducer =
            (eventsProducer, filter) =>
            {
                var wob = new WriteOnceBlock<ResolvedEvent>(x => x);
                eventsProducer.LinkTo(wob, new DataflowLinkOptions { MaxMessages = 1 }, filter);
                return wob;
            };
    }
}
