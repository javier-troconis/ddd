using System;
using System.Collections.Generic;
using System.Linq;

using shared;

namespace eventstore
{
    internal static class TypeExtensions
    {
        public static Type[] GetMessageHandlerTypes(this Type subscriberType)
        {
            return subscriberType
				.GetInterfaces()
				.Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IMessageHandler<,>))
				.ToArray();
        }

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
    }
}
