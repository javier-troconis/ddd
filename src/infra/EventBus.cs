using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;

using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;

using ImpromptuInterface;
using ImpromptuInterface.Dynamic;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using shared;

namespace infra
{
    public delegate Task<ResolvedEvent> HandleResolvedEvent(ResolvedEvent resolvedEvent);

    public sealed class EventBus
	{
		private readonly IEnumerable<Func<Task>> _subscriberRegistrations;
		private readonly string _clusterDns;
		private readonly string _username;
		private readonly string _password;
		private readonly int _externalHttpPort;
		private readonly ILogger _logger;

		public EventBus(string clusterDns, string username, string password, int externalHttpPort, ILogger logger)
			: this(clusterDns, username, password, externalHttpPort, logger, Enumerable.Empty<Func<Task>>())
		{
			
		}

		private EventBus(string clusterDns, string username, string password, int externalHttpPort, ILogger logger, IEnumerable<Func<Task>> subscriberRegistrations)
		{
			_clusterDns = clusterDns;
			_username = username;
			_password = password;
			_externalHttpPort = externalHttpPort;
			_logger = logger;
			_subscriberRegistrations = subscriberRegistrations;
		}

		public EventBus RegisterCatchupSubscriber<TSubscriber>(TSubscriber subscriber, Func<Task<int?>> getCheckpoint, Func<HandleResolvedEvent, HandleResolvedEvent> processHandle = null)
		{
            var handle = (processHandle ?? (x => x))(resolvedEvent => HandleEvent(subscriber, resolvedEvent));
			return new EventBus(_clusterDns, _username, _password, _externalHttpPort, _logger,
				_subscriberRegistrations.Concat(new List<Func<Task>>
				{
					async () =>
					{
						await RegisterSubscriptionStreamAsync<TSubscriber>(new ProjectionManager(_logger, _clusterDns, _externalHttpPort), new UserCredentials(_username, _password));
						await new CatchUpSubscription(
							new EventStoreConnectionFactory(x => x
								.SetDefaultUserCredentials(new UserCredentials(_username, _password))
								.UseConsoleLogger()
								.KeepReconnecting())
								.CreateConnection,
							typeof(TSubscriber).GetEventStoreName(),
							handle.Invoke,
							1000,
							getCheckpoint)
                            .Start();
					}
				}));
		}

		public async Task Start()
		{
			await RegisterByEventTopicProjectionAsync(new ProjectionManager(_logger, _clusterDns, _externalHttpPort), new UserCredentials(_username, _password));
			await Task.WhenAll(_subscriberRegistrations.Select(x => x()));
		}

		internal static async Task<ResolvedEvent> HandleEvent(object subscriber, ResolvedEvent resolvedEvent)
		{
			var recordedEvent = DeserializeEvent(subscriber.GetType(), resolvedEvent);
			await HandleEvent(subscriber, (dynamic)recordedEvent);
			return resolvedEvent;
		}

		internal static Task HandleEvent<TRecordedEvent>(object subscriber, TRecordedEvent recordedEvent)
		{
			var handler = (IMessageHandler<TRecordedEvent, Task>)subscriber;
			return handler.Handle(recordedEvent);
		}

		internal static object DeserializeEvent(Type subscriberType, ResolvedEvent resolvedEvent)
		{
			var eventMetadata = JsonConvert.DeserializeObject<Dictionary<string, object>>(Encoding.UTF8.GetString(resolvedEvent.Event.Metadata));
			var topics = ((JArray)eventMetadata["topics"]).ToObject<object[]>();
			var recordedEventHandlingTypes = subscriberType
				.GetMessageHandlerTypes()
				.Select(x => x.GetGenericArguments()[0]);
			var recordedEventTypes = topics.Join(recordedEventHandlingTypes, x => x, x => x.GetGenericArguments()[0].GetEventStoreName(), (x, y) => y);
			var recordedEventType = recordedEventTypes.First();
			var recordedEvent = new
			{
				resolvedEvent.Event.EventNumber,
				resolvedEvent.Event.EventId,
				resolvedEvent.Event.Created,
				Event = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(resolvedEvent.Event.Data))
			};
			return Impromptu.CoerceConvert(recordedEvent, recordedEventType);
		}

		internal static Task RegisterByEventTopicProjectionAsync(ProjectionManager projectionManager, UserCredentials userCredentials)
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

		internal static Task RegisterSubscriptionStreamAsync<TSubscriber>(ProjectionManager projectionManager, UserCredentials userCredentials)
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

			var subscriberType = typeof(TSubscriber);
			var toStream = subscriberType.GetEventStoreName();
			var projectionName = subscriberType.GetEventStoreName();
			var eventTypes = subscriberType.GetMessageHandlerTypes().Select(x => x.GetGenericArguments()[0].GetGenericArguments()[0]);
			var fromTopics = eventTypes.Select(eventType => $"'{eventType.GetEventStoreName()}'");
			var projectionDefinition = string.Format(projectionDefinitionTemplate, string.Join(",\n", fromTopics), toStream);
			return projectionManager.CreateOrUpdateProjectionAsync(projectionName, projectionDefinition, userCredentials, int.MaxValue);
		}
	}
}
