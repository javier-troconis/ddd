using System;
using System.Collections.Generic;
using System.Linq;

using shared;

namespace eventstore
{
    internal static class TypeExtensions
    {
        public static string GetStreamName(this Type entityType, Guid identity, string category = "")
        {
            var streamName = $"{(EventStoreObjectName)entityType}_{identity.ToString("N").ToLower()}";
            return string.IsNullOrEmpty(category) ? streamName : category + "-" + streamName;
        }

        public static string[] GetEventTopics(this Type eventType)
        {
            return eventType
                .GetInterfaces()
                .Where(x => Attribute.IsDefined(x, typeof(TopicAttribute)))
                .Select(x => ((EventStoreObjectName)x).Value)
                .ToArray();
        }

        public static string GetTopicsProjectionQuery(this Type type)
        {
            var messageHandlerTypes = type.GetMessageHandlerTypes();
            if (!messageHandlerTypes.Any())
            {
                return string.Empty;
            }

            // pull from specific streams ?
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
            var messageHandlingTypes = messageHandlerTypes.Select(x => x.GetGenericArguments()[0].GetGenericArguments()[0]);
            var topics = messageHandlingTypes.Select(handlingType => ((EventStoreObjectName)handlingType).Value);
            var query = string.Format(queryTemplate, string.Join(",\n", topics.Select(topic => $"'{topic}'")), ((EventStoreObjectName)type).Value);
            return query;

        }
    }
}
