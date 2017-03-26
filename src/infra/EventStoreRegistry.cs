using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using EventStore.ClientAPI.SystemData;

namespace infra
{
    public static class EventStoreRegistry
    {
		public static Task RegisterTopicsProjection(ProjectionManager manager)
		{
			const string query =
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

			return manager.CreateContinuousProjection("topics", query, int.MaxValue);
		}

		public static Task RegisterSubscriptionStream<TSubscription>(ProjectionManager manager)
		{
			const string queryTemplate =
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

			var subscriptionType = typeof(TSubscription);
			var subscriptionStream = subscriptionType.GetEventStoreName();
			var projectionName = subscriptionType.GetEventStoreName();
			var eventHandlingTypes = subscriptionType.GetMessageHandlerTypes().Select(x => x.GetGenericArguments()[0].GetGenericArguments()[0]);
			var topics = eventHandlingTypes.Select(eventType => $"'{eventType.GetEventStoreName()}'");
			var query = string.Format(queryTemplate, string.Join(",\n", topics), subscriptionStream);
			return manager.CreateContinuousProjection(projectionName, query, int.MaxValue);
		}

		public static Task RegisterConsumerGroup<TSubscriber>(PersistentSubscriptionManager manager)
		{
			var streamName = typeof(TSubscriber).GetEventStoreName();
			var consumerGroupName = typeof(TSubscriber).GetEventStoreName();
			return manager.CreatePersistentSubscription(streamName, consumerGroupName);
		}
	}
}
