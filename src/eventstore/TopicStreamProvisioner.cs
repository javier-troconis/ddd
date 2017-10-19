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
				@"function emitTopic(e) {{
    return function(topic) {{
           var message = {{ streamId: '{0}-' + topic, eventName: topic, body: e.sequenceNumber + '@' + e.streamId, isJson: false }};
           eventProcessor.emit(message);
    }};
}}

fromAll()
    .when({{
        $any: function(s, e) {{
            var topics;
            if (e.streamId.indexOf('{0}') === 0  || !e.metadata || !(topics = e.metadata.topics)) {{
                return;
            }}
            topics.forEach(emitTopic(e));
        }}
    }});";

			var query = string.Format(queryTemplate, queryName);
            return _provisioningTasksQueue.SendToChannel(() => _projectionManager.CreateOrUpdateContinuousProjection(queryName, query), queryName);
        }
    }
}
