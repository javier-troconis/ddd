using System;
using System.Collections.Generic;
using System.Linq;

using shared;

namespace eventstore
{
    internal static class TypeExtensions
    {
        public static IEnumerable<Type> GetMessageHandlerTypes(this Type subscriberType)
        {
            return subscriberType
				.GetInterfaces()
				.Where(i => i.IsGenericType 
					&& i.GetGenericTypeDefinition() == typeof(IMessageHandler<,>));
        }

        public static string GetStreamName(this Type entityType, Guid identity, string category = "")
        {
            var streamName = $"{entityType.GetEventStoreName()}_{identity.ToString("N").ToLower()}";
	        return string.IsNullOrEmpty(category) ? streamName : category + "-" + streamName;
        }

        public static string GetEventStoreName(this Type type)
        {
            return type.FullName.Replace('.', '_');
        }

		public static IEnumerable<string> GetEventTopics(this Type eventType)
        {
	        return eventType
		        .GetInterfaces()
		        .Select(x => x.GetEventStoreName());
        }
    }
}
