using infra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using contracts;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Common.Log;
using EventStore.ClientAPI.Exceptions;
using EventStore.ClientAPI.Projections;
using EventStore.ClientAPI.SystemData;

using Newtonsoft.Json;

using shared;

namespace subscriber
{
    public class Program : IMessageHandler<IApplicationStarted>
    {
        public static Task RegisterByEventTopicProjectionAsync(ProjectionManager projectionManager, UserCredentials userCredentials)
        {
            const string projectionDefinitionTemplate =
                @"function emitTopic(e) {
    return function(topic) {
        var metadata = Object.keys(e.metadata).reduce(function(x, y) {
            if(y[0] === '$'){
                return x;
            }
            x[y] = e.metadata[y];
            return x;
        }, {});
        emit('topic-' + topic, topic, e.body, metadata);
    }
}

fromAll()
    .whenAny(function(s, e) {
        var topics;
        if (e.streamId === 'topic-' + e.eventType || !e.metadata || !(topics = e.metadata.topics)) {
            return;
        }
        topics.forEach(emitTopic(e));
    });";
            return projectionManager.CreateOrUpdateProjectionAsync("by_event_topic", projectionDefinitionTemplate, userCredentials, int.MaxValue);
        }

        public static Task RegisterSubscriptionStreamAsync(ProjectionManager projectionManager, UserCredentials userCredentials, Type subscriberType)
        {
            const string projectionDefinitionTemplate =
                @"var topics = [{0}];

function handle(s, e) {{
    var targetTopics = e.metadata.topics.filter(function(topic) {{
        return topics.indexOf(topic) >= 0;
    }});
    if(targetTopics[targetTopics.length - 1] === e.eventType) {{
        linkTo('{1}', e);
    }}
}}

var handlers = topics.reduce(function(x, y) {{
    x[y] = handle;
    return x;
}}, {{}});

fromCategory('topic')
    .when(handlers);";

            var toStream = subscriberType.GetEventStoreName();
            var projectionName = subscriberType.GetEventStoreName();
            var eventTypes = subscriberType.GetMessageHandlerTypes().Select(x => x.GetGenericArguments()[0]);
            var fromTopics = eventTypes.Select(eventType => $"'{eventType.GetEventStoreName()}'");
            var projectionDefinition = string.Format(projectionDefinitionTemplate, string.Join(",\n", fromTopics), toStream);
            return projectionManager.CreateOrUpdateProjectionAsync(projectionName, projectionDefinition, userCredentials, int.MaxValue);
        }


        public static void Main(string[] args)
        {
            var projectionManager = new ProjectionManager(new ConsoleLogger(), EventStoreSettings.ClusterDns, EventStoreSettings.ExternalHttpPort);

            RegisterByEventTopicProjectionAsync(projectionManager, EventStoreSettings.Credentials).Wait();
            RegisterSubscriptionStreamAsync(projectionManager, EventStoreSettings.Credentials, typeof(Program)).Wait();

            //var persistentSubscription = new PersistentSubscription(
            //    new EventStoreConnectionFactory(x => x.SetDefaultUserCredentials(EventStoreSettings.Credentials).UseConsoleLogger().KeepReconnecting()),
            //        _subscriptionStreamName, _subscriptionGroupName,
            //    async e =>
            //    {
            //        Console.WriteLine($"persistent subscription processed stream: {e.Event.EventStreamId} | event: {e.OriginalEventNumber} - {e.Event.EventType} - {e.Event.EventId}");
            //        await Task.Delay(500);
            //        return await Task.FromResult(true);
            //    }, 1000, new ConsoleLogger());
            //persistentSubscription.StartAsync().Wait();

            new CatchUpSubscription(
                new EventStoreConnectionFactory(x => x.SetDefaultUserCredentials(EventStoreSettings.Credentials).UseConsoleLogger().KeepReconnecting()),
                typeof(Program).GetEventStoreName(),
                e =>
                {
                    Console.WriteLine($"catchup subscription : {e.Event.EventStreamId} | event: {e.OriginalEventNumber} - {e.Event.EventType} - {e.Event.EventId} | thread : {Thread.CurrentThread.GetHashCode()}");
                   
                    return Task.FromResult(true);
                }, 1000, () => Task.FromResult(default(int?)), new ConsoleLogger())
                .StartAsync().Wait();

            while (true);
        }

        public void Handle(IApplicationStarted message)
        {
            throw new NotImplementedException();
        }
    }
}
