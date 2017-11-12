using shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eventstore
{
	public enum ProvisionSubscriptionStreamResult
	{
		NotFound,
		Provisioned
	}

	public interface ISubscriptionStreamProvisioningService
    {
        ISubscriptionStreamProvisioningService RegisterSubscriptionStream<TSubscription>() where TSubscription : IMessageHandler;
        Task<ProvisionSubscriptionStreamResult> ProvisionSubscriptionStream(string subscriptionStreamName);
	    Task ProvisionAllSubscriptionStreams();
	}

    public sealed class SubscriptionStreamProvisioningService : ISubscriptionStreamProvisioningService
    {
        private readonly IProjectionManager _projectionManager;
        private readonly IDictionary<string, Func<Task>> _registry;

        public SubscriptionStreamProvisioningService(IProjectionManager projectionManager)
            : this
			(
				  projectionManager, 
				  new Dictionary<string, Func<Task>>()
			)
        {

        }

        private SubscriptionStreamProvisioningService
			(
				IProjectionManager projectionManager,
				IDictionary<string, Func<Task>> registry
			)
        {
            _projectionManager = projectionManager;
            _registry = registry;
        }

        public ISubscriptionStreamProvisioningService RegisterSubscriptionStream<TSubscription>() where TSubscription : IMessageHandler
        {
	        return new SubscriptionStreamProvisioningService(
		        _projectionManager,
		        _registry.Merge(new Dictionary<string, Func<Task>>
		        {
			        {
				        typeof(TSubscription).GetEventStoreName(),
				        () =>
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
					        var subscriptionType = typeof(TSubscription);
					        var handlingTypes = subscriptionType.GetMessageHandlerTypes()
						        .Select(x => x.GetGenericArguments()[0].GetGenericArguments()[0]);
					        var topics = handlingTypes.Select(handlingType => handlingType.GetEventStoreName());
					        var query = string.Format(queryTemplate, string.Join(",\n", topics.Select(topic => $"'{topic}'")),
						        typeof(TSubscription).GetEventStoreName());
					        return _projectionManager.CreateOrUpdateContinuousProjection(typeof(TSubscription).GetEventStoreName(),
						        query);
				        }
			        }
		        })
			);
        }

	    public async Task<ProvisionSubscriptionStreamResult> ProvisionSubscriptionStream(string subscriptionStreamName)
	    {
		    if (!_registry.TryGetValue(subscriptionStreamName, out Func<Task> operation))
		    {
			    return ProvisionSubscriptionStreamResult.NotFound;
		    }
		    await operation();
			return ProvisionSubscriptionStreamResult.Provisioned;
	    }

	    public Task ProvisionAllSubscriptionStreams()
	    {
		    return Task.WhenAll(_registry.Select(x => ProvisionSubscriptionStream(x.Key)));
	    }
    }
}
