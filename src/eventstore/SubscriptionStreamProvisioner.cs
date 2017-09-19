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
        Task ProvisionSubscriptionStreams(string targetSubscriptionStreamName = "*");
    }

    public class SubscriptionStreamProvisioner : ISubscriptionStreamProvisioner
    {
        private static readonly TaskQueue _provisioningTasksQueue = new TaskQueue();
        private readonly IProjectionManager _projectionManager;
        private readonly IEnumerable<Func<string, Task>> _provisioningTasks;

        public SubscriptionStreamProvisioner(IProjectionManager projectionManager)
            : this(projectionManager, Enumerable.Empty<Func<string, Task>>())
        {
        }

        private SubscriptionStreamProvisioner(
            IProjectionManager projectionManager,
            IEnumerable<Func<string, Task>> provisioningTasks
            )
        {
            _projectionManager = projectionManager;
            _provisioningTasks = provisioningTasks;
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

            return new SubscriptionStreamProvisioner(
                _projectionManager,
                _provisioningTasks.Concat(
                    new Func<string, Task>[] {
                        targetSubscriptionStreamName =>
                            {
                                var subscriptionStreamName = typeof(TSubscription).GetEventStoreName();
                                if(!subscriptionStreamName.MatchesWildcard(targetSubscriptionStreamName))
                                {
                                    return Task.CompletedTask;
                                }
                                var subscriptionType = typeof(TSubscription);
                                var handlingTypes = subscriptionType.GetMessageHandlerTypes().Select(x => x.GetGenericArguments()[0].GetGenericArguments()[0]);
                                var topics = handlingTypes.Select(handlingType => handlingType.GetEventStoreName());
                                var query = string.Format(queryTemplate, string.Join(",\n", topics.Select(topic => $"'{topic}'")), subscriptionStreamName);
                                return _provisioningTasksQueue.SendToChannelAsync(subscriptionStreamName,
                                    () => _projectionManager.CreateOrUpdateContinuousProjection(subscriptionStreamName, query));
                            }
                    })
            );
        }

        public Task ProvisionSubscriptionStreams(string targetSubscriptionStreamName = "*")
        {
            return Task.WhenAll(_provisioningTasks.Select(x => x(targetSubscriptionStreamName)));
        }
    }
}
