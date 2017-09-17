using shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eventstore
{
    public interface ISubscriptionStreamProvisioner
    {
        ISubscriptionStreamProvisioner RegisterSubscriptionStreamProvisioning<TSubscription>() where TSubscription : IMessageHandler;
        Task ProvisionSubscriptionStreams(string subscriptionStreamName = "*");
    }

    public class SubscriptionStreamProvisioner : ISubscriptionStreamProvisioner
    {
        private static readonly TaskQueue _provisioningTasksQueue = new TaskQueue();
        private readonly IProjectionManager _projectionManager;
        private readonly IDictionary<string, Func<Task>> _provisioningTask;

        public SubscriptionStreamProvisioner(IProjectionManager projectionManager)
			: this(projectionManager, new Dictionary<string, Func<Task>>())
		{
        }

        private SubscriptionStreamProvisioner(
            IProjectionManager projectionManager,
            IDictionary<string, Func<Task>> provisioningTask
            )
        {
            _projectionManager = projectionManager;
            _provisioningTask = provisioningTask;
        }

        public ISubscriptionStreamProvisioner RegisterSubscriptionStreamProvisioning<TSubscription>() where TSubscription : IMessageHandler
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
		$init: function() {{
			return {{ lastEvent: ''}};
		}}
	}});

fromAll()
    .when(handlers);";

            return new SubscriptionStreamProvisioner(_projectionManager, new Dictionary<string, Func<Task>>(_provisioningTask)
            {
                {
                    typeof(TSubscription).GetEventStoreName(),
                    () =>
                    {
                        var subscriptionType = typeof(TSubscription);
                        var subscriptionName = subscriptionType.GetEventStoreName();
                        var handlingTypes = subscriptionType.GetMessageHandlerTypes().Select(x => x.GetGenericArguments()[0].GetGenericArguments()[0]);
                        var topics = handlingTypes.Select(handlingType => handlingType.GetEventStoreName());
                        var query = string.Format(queryTemplate, string.Join(",\n", topics.Select(topic => $"'{topic}'")), subscriptionName);
                        return _projectionManager.CreateOrUpdateContinuousProjection(subscriptionName, query);
                    }
                }
            });
        }

        public Task ProvisionSubscriptionStreams(string subscriptionStreamName = "*")
        {
            return Task.WhenAll(
                _provisioningTask
                    .Where(x => x.Key.MatchesWildcard(subscriptionStreamName))
                    .Select(x => _provisioningTasksQueue.SendToChannelAsync(x.Key, x.Value))
                );
        }
    }
}
