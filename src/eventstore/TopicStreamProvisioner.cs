using shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eventstore
{
    public interface ITopicStreamProvisioner
    {
        Task ProvisionTopicStream();
    }

    public class TopicStreamProvisioner : ITopicStreamProvisioner
    {
        private static readonly TaskQueue _provisioningTasksQueue = new TaskQueue();
        private readonly IProjectionManager _projectionManager;

        public TopicStreamProvisioner(
            IProjectionManager projectionManager
            )
        {
            _projectionManager = projectionManager;
        }

        public Task ProvisionTopicStream()
        {
            const string queryName = "topic";
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

            var query = string.Format(queryTemplate, queryName);
            return _provisioningTasksQueue.SendToChannelAsync(queryName, () => _projectionManager.CreateOrUpdateContinuousProjection(queryName, query));
        }
    }
}
