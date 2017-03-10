using infra;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using contracts;

using Elasticsearch.Net;

using EventStore.ClientAPI;
using EventStore.ClientAPI.Common.Log;
using EventStore.ClientAPI.Exceptions;
using EventStore.ClientAPI.Projections;
using EventStore.ClientAPI.SystemData;

using ImpromptuInterface;

using Nest;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using shared;

namespace subscriber
{
    public class Program : IMessageHandler<IApplicationStarted>, IMessageHandler<IApplicationSubmitted>
    {
        public static Task RegisterByEventTopicProjectionAsync(ProjectionManager projectionManager, UserCredentials userCredentials)
        {
            const string projectionDefinitionTemplate =
               @"function emitTopic(e) {
    return function(topic) {
           var message = { streamId: 'topics', eventName: topic, body: e.sequenceNumber + '@' + e.streamId, isJson: false };
           eventProcessor.emit(message);
    };
}

fromAll()
    .when({
        $any: function(s, e) {
            var topics;
            if (e.streamId === 'topics' || !e.metadata || !(topics = e.metadata.topics)) {
                return;
            }
            topics.forEach(emitTopic(e));
        }
    });";
            return projectionManager.CreateOrUpdateProjectionAsync("topics", projectionDefinitionTemplate, userCredentials, int.MaxValue);
        }

        public static Task RegisterSubscriptionStreamAsync(ProjectionManager projectionManager, UserCredentials userCredentials, Type subscriberType)
        {
			const string projectionDefinitionTemplate =
				@"var topics = [{0}];

function handle(s, e) {{
    var event = e.bodyRaw;
    if(event !== s.lastEvent) {{ 
        var message = {{ streamId: '{1}', eventName: '$>', body: event, isJson: false }};
        eventProcessor.emit(message);
    }}
	s.lastEvent = event;
}}

var handlers = topics.reduce(
    function(x, y) {{
        x[y] = handle;
        return x;
    }}, 
	{{
		$init: function(){{
			return {{ lastEvent: ''}};
		}}
	}});

fromStream('topics')
    .when(handlers);";

			var toStream = subscriberType.GetEventStoreName();
            var projectionName = subscriberType.GetEventStoreName();
            var eventTypes = subscriberType.GetMessageHandlerTypes().Select(x => x.GetGenericArguments()[0]);
            var fromTopics = eventTypes.Select(eventType => $"'{eventType.GetEventStoreName()}'");
            var projectionDefinition = string.Format(projectionDefinitionTemplate, string.Join(",\n", fromTopics), toStream);
            return projectionManager.CreateOrUpdateProjectionAsync(projectionName, projectionDefinition, userCredentials, int.MaxValue);
        }

        internal static IEvent DeserializeEvent(Type subscriberType, ResolvedEvent resolvedEvent)
        {
            var eventMetadata = JsonConvert.DeserializeObject<Dictionary<string, object>>(Encoding.UTF8.GetString(resolvedEvent.Event.Metadata));
            var topics = ((JArray)eventMetadata["topics"]).ToObject<string[]>();
            var candidateEventTypes = subscriberType
               .GetMessageHandlerTypes()
               .Select(x => x.GetGenericArguments()[0]);
            var eventType = topics.Join(candidateEventTypes, x => x, x => x.GetEventStoreName(), (x, y) => y).First();
            dynamic eventData = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(resolvedEvent.Event.Data));
            return Impromptu.CoerceConvert(eventData, eventType);
        }

        public static async Task<int?> GetDocumentTypeVersion<TDocument>(IElasticClient elasticClient) where TDocument : class
        {
            const string maxVersionQueryKey = "max_version";
            const string elasticVersionFieldName = "_version";
            var searchResponse = await elasticClient.SearchAsync<TDocument>(s => s
                .Size(0)
                .Aggregations(agregateSelector => agregateSelector
                    .Max(maxVersionQueryKey, maxSelector => maxSelector
                        .Field(elasticVersionFieldName))));
            var maxVersionAggregation = searchResponse.Aggs.Max(maxVersionQueryKey);
            var maxVersion = maxVersionAggregation.Value;
            return (int?)maxVersion;
        }

        private static Task MapType<TDocument>(IElasticClient elasticClient) where TDocument : class
        {
            return elasticClient.MapAsync<TDocument>(mapping => mapping
                .AutoMap());
        }

        public static async Task IndexAsync<TDocument>(IElasticClient elasticClient, TDocument document, long version) where TDocument : class
        {
            try
            {
                await elasticClient.IndexAsync(document, s => s
                    .VersionType(VersionType.External)
                    .Version(version)
                    .Refresh()
                    );
            }
            catch (ElasticsearchClientException ex) when (ex.Response.ServerError.Status == (int)HttpStatusCode.Conflict)
            {
            }
        }

       

        public static async Task UpdateAsync<TDocument>(IElasticClient elasticClient, TDocument documentSample, Action<TDocument> updateDocument, long version) where TDocument : class
        {
            IGetResponse<TDocument> getResponse = await elasticClient.GetAsync<TDocument>(documentSample);
            updateDocument(getResponse.Source);
            await IndexAsync(elasticClient, getResponse.Source, version);
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

            //var elasticClient = new ElasticClientBuilder(new[] { new Uri("http://localhost:9200") }, "admin", "admin")
            //    .MapDefaultTypeIndices(typeof(Program).Assembly)
            //    .Create();
            

            //try
            //{
            //    elasticClient.CreateIndexAsync(elasticIndex).Wait();
            //    MapType<TestDocument>(elasticClient, elasticIndex).Wait();
            //}
            //catch
            //{

            //}

            //var taskQueue = new TaskQueue();

            //new CatchUpSubscription(
            //    new EventStoreConnectionFactory(x => x.SetDefaultUserCredentials(EventStoreSettings.Credentials).UseConsoleLogger().KeepReconnecting()),
            //    typeof(Program).GetEventStoreName(),
            //    e =>
            //    {
            //        object streamId;
            //        var eventMetadata = JsonConvert.DeserializeObject<IDictionary<string, object>>(Encoding.UTF8.GetString(e.Event.Metadata));
            //        if (!eventMetadata.TryGetValue("streamId", out streamId))
            //        {
            //            return Task.FromResult(true);
            //        }
            //        return taskQueue.SendToChannelAsync(elasticIndex, () =>
            //        {
            //            Console.WriteLine($"processing - {streamId} | {e.OriginalEventNumber}");
            //            if (string.Equals(e.Event.EventType, typeof(IApplicationStarted).GetEventStoreName()))
            //            {
            //                return IndexAsync(elasticClient, elasticIndex, new TestDocument { Id = streamId.ToString(), Value = e.OriginalEventNumber }, e.OriginalEventNumber);
            //            }
            //            return UpdateAsync(elasticClient, elasticIndex, new TestDocument { Id = streamId.ToString() }, x => x.Value = e.OriginalEventNumber, e.OriginalEventNumber);
            //        }, x => Console.WriteLine($"processed - {streamId} | {e.OriginalEventNumber}"), (x, y) => Console.WriteLine("failed"));
            //    }, 1000, () => GetDocumentTypeVersion<TestDocument>(elasticClient, elasticIndex), new ConsoleLogger())
            //    .StartAsync().Wait();

            new CatchUpSubscription(
                new EventStoreConnectionFactory(x => x.SetDefaultUserCredentials(EventStoreSettings.Credentials).UseConsoleLogger().KeepReconnecting()),
                typeof(Program).GetEventStoreName(),
                e =>
                {
                    Console.WriteLine(e.Event.EventId);
                    Console.WriteLine();                    
                    return Task.FromResult(true);
                }, 1000, () => Task.FromResult(default(int?)), new ConsoleLogger())
                .StartAsync().Wait();

            while (true)
            {
            }
        }

        public void Handle(IApplicationStarted message)
        {
            throw new NotImplementedException();
        }

        public void Handle(IApplicationSubmitted message)
        {
            throw new NotImplementedException();
        }
    }

    [ElasticsearchType]
    public class TestDocument
    {
        public string Id { get; set; }
        public long Value { get; set; }
    }
}
