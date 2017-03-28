using System.Linq;
using System.Threading.Tasks;

using EventStore.ClientAPI.Exceptions;

namespace eventstore
{

	

	public static class EventStoreRegistry
    {
	    public static async Task RegisterSubscriptionStream<TSubscription>(ProjectionManager projectionManager)
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
			var subscriptionName = subscriptionType.GetEventStoreName();
			var handlingTypes = subscriptionType.GetMessageHandlerTypes().Select(x => x.GetGenericArguments()[0].GetGenericArguments()[0]);
			var topics = handlingTypes.Select(handlingType => handlingType.GetEventStoreName());
			var query = string.Format(queryTemplate, string.Join(",\n", topics.Select(topic => $"'{topic}'")), subscriptionName);
			try
			{
				await projectionManager.CreateContinuousProjection(subscriptionName, query, int.MaxValue);
			}
			catch (ProjectionCommandConflictException)
			{
				await projectionManager.UpdateProjection(subscriptionName, query, int.MaxValue);
			}
	    }

		public static Task RegisterPersistentSubscription<TSubscription>(PersistentSubscriptionManager persistentSubscriptionManager)
		{
			string streamName = typeof(TSubscription).GetEventStoreName();
			string groupName = typeof(TSubscription).GetEventStoreName();
			return persistentSubscriptionManager.CreatePersistentSubscription(streamName, groupName);
		}

		public static async Task RegisterTopicsStream(ProjectionManager manager)
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
			try
			{
				await manager.CreateContinuousProjection("topics", query, int.MaxValue);
			}
			catch (ProjectionCommandConflictException)
			{
				await manager.UpdateProjection("topics", query, int.MaxValue);
			}
		}

	}
}
