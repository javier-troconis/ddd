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
    public class Program : IMessageHandler<IApplicationStarted, Task>, IMessageHandler<IApplicationSubmitted, Task>
    {
        static Task RegisterByEventTopicProjectionAsync(ProjectionManager projectionManager, UserCredentials userCredentials)
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

        static Task RegisterSubscriptionStreamAsync(ProjectionManager projectionManager, UserCredentials userCredentials, Type subscriberType)
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

        static IEvent DeserializeEvent<TSubscriber>(ResolvedEvent resolvedEvent)
        {
            var eventMetadata = JsonConvert.DeserializeObject<Dictionary<string, object>>(Encoding.UTF8.GetString(resolvedEvent.Event.Metadata));
            var topics = ((JArray)eventMetadata["topics"]).ToObject<string[]>();
            var candidateEventTypes = typeof(TSubscriber)
			   .GetMessageHandlerTypes()
               .Select(x => x.GetGenericArguments()[0]);
            var eventType = topics.Join(candidateEventTypes, x => x, x => x.GetEventStoreName(), (x, y) => y).First();
            dynamic eventData = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(resolvedEvent.Event.Data));
            return Impromptu.CoerceConvert(eventData, eventType);
        }

		//static async Task<int?> GetDocumentTypeVersion<TDocument>(IElasticClient elasticClient) where TDocument : class
		//{
		//    const string maxVersionQueryKey = "max_version";
		//    const string elasticVersionFieldName = "_version";
		//    var searchResponse = await elasticClient.SearchAsync<TDocument>(s => s
		//        .Size(0)
		//        .Aggregations(agregateSelector => agregateSelector
		//            .Max(maxVersionQueryKey, maxSelector => maxSelector
		//                .Field(elasticVersionFieldName))));
		//    var maxVersionAggregation = searchResponse.Aggs.Max(maxVersionQueryKey);
		//    var maxVersion = maxVersionAggregation.Value;
		//    return (int?)maxVersion;
		//}

		//private static Task MapType<TDocument>(IElasticClient elasticClient) where TDocument : class
		//{
		//    return elasticClient.MapAsync<TDocument>(mapping => mapping
		//        .AutoMap());
		//}

		static async Task IndexAsync<TDocument>(IElasticClient elasticClient, TDocument document, long version) where TDocument : class
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

       

        static async Task UpdateAsync<TDocument>(IElasticClient elasticClient, Guid documentId, Action<TDocument> updateDocument, long expectedVersion) where TDocument : class
        {
            IGetResponse<TDocument> getResponse = await elasticClient.GetAsync<TDocument>(documentId);
            updateDocument(getResponse.Source);
            await IndexAsync(elasticClient, getResponse.Source, expectedVersion + 1);
        }

        public static void Main(string[] args)
        {
            var projectionManager = new ProjectionManager(new ConsoleLogger(), EventStoreSettings.ClusterDns, EventStoreSettings.ExternalHttpPort);

            RegisterByEventTopicProjectionAsync(projectionManager, EventStoreSettings.Credentials).Wait();
            RegisterSubscriptionStreamAsync(projectionManager, EventStoreSettings.Credentials, typeof(Program)).Wait();

	        var handle = HandleEvent(() => new Program(new ElasticClient())).ComposeForward(_writeCheckpoint.ToAsync());

			new CatchUpSubscription(
					new EventStoreConnectionFactory(x => x.SetDefaultUserCredentials(EventStoreSettings.Credentials).UseConsoleLogger().KeepReconnecting()).CreateConnection,
					typeof(Program).GetEventStoreName(), 
					handle,
					1000, 
					() => Task.FromResult(default(int?)))
                .StartAsync()
				.Wait();

            while (true)
            {

            }
        }

	    static Func<ResolvedEvent, Task<ResolvedEvent>> HandleEvent<TSubscriber>(Func<TSubscriber> createSubscriber)
	    {
			var subscriber = createSubscriber();
		    return async resolvedEvent =>
		    {
				var @event = DeserializeEvent<TSubscriber>(resolvedEvent);
				await HandleEvent(subscriber, (dynamic)@event);
				return resolvedEvent;
		    };
	    }

		static Task HandleEvent<TEvent>(object subscriber, TEvent @event) where TEvent : IEvent
		{
			var handler = (IMessageHandler<TEvent, Task>)subscriber;
			return handler.Handle(@event);
		}

		private static readonly Func<ResolvedEvent, ResolvedEvent> _writeCheckpoint = resolvedEvent =>
		{
			Console.WriteLine("wrote checkpoint: " + resolvedEvent.OriginalEventNumber);
			return resolvedEvent;
		};

	    readonly IElasticClient _elasticClient;

		public Program(IElasticClient elasticClient)
		{
			_elasticClient = elasticClient;
		}

		public Task Handle(IApplicationStarted message)
        {
	        return Task.CompletedTask;
        }

        public Task Handle(IApplicationSubmitted message)
        {
			return Task.CompletedTask;
		}
    }

    [ElasticsearchType]
    public class TestDocument
    {
        public string Id { get; set; }
        public long Value { get; set; }
    }
}
