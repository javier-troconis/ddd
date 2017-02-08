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
            return subscriberType.GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMessageHandler<>));
        }

        public static string[] GetEventTopics(this IEvent @event)
        {
            return EventTopic.GetAllTopics(@event);
        }

        public static string GetEventStoreName(this Type type)
        {
            return type.FullName.Replace('.', '_');
        }

        private static class EventTopic
        {
            private static readonly ConcurrentDictionary<Type, string[]> _eventTopicsCache = new ConcurrentDictionary<Type, string[]>();
            private static readonly Type _baseEventType = typeof(IEvent);

            public static string[] GetAllTopics(IEvent @event)
            {
                return _eventTopicsCache.GetOrAdd(@event.GetType(), CreateTopics);
            }

            private static string[] CreateTopics(Type eventType)
            {
                return eventType
                    .GetInterfaces()
                    .Where(x => x != _baseEventType && _baseEventType.IsAssignableFrom(x))
                    .Select(x => x.GetEventStoreName())
                    .ToArray();
            }
        }
    }
}
