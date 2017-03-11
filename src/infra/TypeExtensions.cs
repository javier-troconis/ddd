using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using shared;

namespace infra
{
    public static class TypeExtensions
    {
        public static IEnumerable<Type> GetMessageHandlerTypes(this Type subscriberType)
        {
            return subscriberType.GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMessageHandler<,>));
        }

        public static string GetStreamName(this Type entityType, Guid entityId, string streamCategory = "")
        {
            var streamName = $"{entityType.GetEventStoreName()}_{entityId.ToString("N").ToLower()}";
            if (!string.IsNullOrEmpty(streamCategory))
            {
                streamName = streamCategory + "-" + streamName;
            }
            return streamName;
        }

        public static string GetEventStoreName(this Type type)
        {
            return type.FullName.Replace('.', '_');
        }

        public static string[] GetEventTopics(this Type eventType)
        {
            var baseEventType = typeof(IEvent);
            return eventType
                .GetInterfaces()
                .Where(x => x != baseEventType && baseEventType.IsAssignableFrom(x))
                .Select(x => x.GetEventStoreName())
                .ToArray();
        }
    }
}
