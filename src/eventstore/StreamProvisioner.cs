using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Net;

using EventStore.ClientAPI.Exceptions;

using shared;

namespace eventstore
{
	public interface ISystemStreamsProvisioner
	{
		Task ProvisionSystemStreams();
	}

	public interface ISubscriptionStreamProvisioner
	{
		ISubscriptionStreamProvisioner IncludeSubscriptionStream<TSubscription>() where TSubscription : IMessageHandler;
		Task ProvisionSubscriptionStreams(string subscriptionStream = "*");
	}

	public class StreamProvisioner : ISystemStreamsProvisioner, ISubscriptionStreamProvisioner
	{
		private static readonly TaskQueue _provisioningTasksQueue = new TaskQueue();
		private readonly IProjectionManager _projectionManager;
		private readonly IDictionary<string, Func<Task>> _provisioningTask;

		public StreamProvisioner(IProjectionManager projectionManager)
			: this(projectionManager, new Dictionary<string, Func<Task>>())
		{
		}

		private StreamProvisioner(
			IProjectionManager projectionManager,
			IDictionary<string, Func<Task>> provisioningTask
			)
		{
			_projectionManager = projectionManager;
			_provisioningTask = provisioningTask;
		}

		public Task ProvisionSystemStreams()
		{
			const string projectionQueryTemplate =
				@"function emitTopic(e) {{
    return function(topic) {{
           var message = {{ streamId: '{0}', eventName: topic, body: e.sequenceNumber + '@' + e.streamId, isJson: false }};
           eventProcessor.emit(message);
    }};
}}

fromAll()
    .when({{
        $any: function(s, e) {{
            var topics;
            if (e.streamId === '{0}' || !e.metadata || !(topics = e.metadata.topics)) {{
                return;
            }}
            topics.forEach(emitTopic(e));
        }}
    }});";
			var projectionQuery = string.Format(projectionQueryTemplate, StreamName.Topics);
			return Task.WhenAll(
				_provisioningTasksQueue.SendToChannelAsync(StreamName.Topics, () => _projectionManager.CreateOrUpdateContinuousProjection(StreamName.Topics, projectionQuery)),
				IncludeSubscriptionStream<IPersistentSubscriptionsProvisioningRequests>()
					.IncludeSubscriptionStream<ISubscriptionStreamsProvisioningRequests>()
						.ProvisionSubscriptionStreams()
				);
		}

		public ISubscriptionStreamProvisioner IncludeSubscriptionStream<TSubscription>() where TSubscription : IMessageHandler
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

fromStream('{2}')
    .when(handlers);";
			return new StreamProvisioner(_projectionManager, new Dictionary<string, Func<Task>>(_provisioningTask)
			{
				{
					typeof(TSubscription).GetEventStoreName(),
					() =>
					{
						var subscriptionType = typeof(TSubscription);
						var subscriptionName = subscriptionType.GetEventStoreName();
						var handlingTypes = subscriptionType.GetMessageHandlerTypes().Select(x => x.GetGenericArguments()[0].GetGenericArguments()[0]);
						var topics = handlingTypes.Select(handlingType => handlingType.GetEventStoreName());
						var query = string.Format(queryTemplate, string.Join(",\n", topics.Select(topic => $"'{topic}'")), subscriptionName, StreamName.Topics);
						return _projectionManager.CreateOrUpdateContinuousProjection(subscriptionName, query);
					}
				}
			});
		}

		public Task ProvisionSubscriptionStreams(string subscriptionStream = "*")
		{
			return Task.WhenAll(
				_provisioningTask
					.Where(x => x.Key.MatchesWildcard(subscriptionStream))
					.Select(x => _provisioningTasksQueue.SendToChannelAsync(x.Key, x.Value))
				);
		}
	}
}
