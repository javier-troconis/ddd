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
	public sealed class EventBus
	{
		private readonly Dictionary<Type, Func<Task>> _subscriberRegistrations = new Dictionary<Type, Func<Task>>();
		private readonly string _clusterDns;
		private readonly string _username;
		private readonly string _password;
		private readonly int _externalHttpPort;
		private readonly ILogger _logger;

		public EventBus(string clusterDns, string username, string password, int externalHttpPort, ILogger logger)
		{
			_clusterDns = clusterDns;
			_username = username;
			_password = password;
			_externalHttpPort = externalHttpPort;
			_logger = logger;
		}

		private EventBus(string clusterDns, string username, string password, int externalHttpPort, ILogger logger, Dictionary<Type, Func<Task>> subscriberRegistrations)
			: this(clusterDns, username, password, externalHttpPort, logger)
		{
			_subscriberRegistrations = subscriberRegistrations;
		}

		public EventBus RegisterCatchupSubscriber<TSubscriber>(TSubscriber subscriber, Func<Task<int?>> getCheckpoint, Func<Func<ResolvedEvent, Task<ResolvedEvent>>, Func<ResolvedEvent, Task<ResolvedEvent>>> processEventHandling = null)
		{
			processEventHandling = processEventHandling ?? (x => x);
			return new EventBus(_clusterDns, _username, _password, _externalHttpPort, _logger,
				new Dictionary<Type, Func<Task>>(_subscriberRegistrations)
				{
					{
						typeof(TSubscriber),
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
									processEventHandling(resolvedEvent => HandleEvent(subscriber, resolvedEvent)),
									1000,
									getCheckpoint)
								.StartAsync();
						}
					}
				});
		}

		public async Task Start()
		{
			await RegisterByEventTopicProjectionAsync(new ProjectionManager(_logger, _clusterDns, _externalHttpPort), new UserCredentials(_username, _password));
			await Task.WhenAll(_subscriberRegistrations.Values.Select(x => x()));
		}

		internal static async Task<ResolvedEvent> HandleEvent(object subscriber, ResolvedEvent resolvedEvent)
		{
			var @event = DeserializeEvent(subscriber.GetType(), resolvedEvent);
			await HandleEvent(subscriber, (dynamic)@event);
			return resolvedEvent;
		}

		internal static Task HandleEvent<TEvent>(object subscriber, TEvent @event) where TEvent : IEvent
		{
			var handler = (IMessageHandler<TEvent, Task>)subscriber;
			return handler.Handle(@event);
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
			var eventTypes = subscriberType.GetMessageHandlerTypes().Select(x => x.GetGenericArguments()[0]);
			var fromTopics = eventTypes.Select(eventType => $"'{eventType.GetEventStoreName()}'");
			var projectionDefinition = string.Format(projectionDefinitionTemplate, string.Join(",\n", fromTopics), toStream);
			return projectionManager.CreateOrUpdateProjectionAsync(projectionName, projectionDefinition, userCredentials, int.MaxValue);
		}
	}
}
