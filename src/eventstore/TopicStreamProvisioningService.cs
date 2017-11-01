using shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eventstore
{
    public interface ITopicStreamProvisioningService
    {
        Task ProvisionTopicStream();
    }

    public class TopicStreamProvisioningService : ITopicStreamProvisioningService
    {
        private readonly IProjectionManager _projectionManager;

        public TopicStreamProvisioningService(
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
            return _projectionManager.CreateOrUpdateContinuousProjection(queryName, query);
        }
    }
}
