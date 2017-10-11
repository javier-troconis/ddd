﻿using System;
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
				.Where(x => 
					x.IsGenericType 
						&& x.GetGenericTypeDefinition() == typeof(IMessageHandler<,>))
				.ToArray();
        }

        public static string GetStreamName(this Type entityType, Guid identity, string category = "")
        {
            var streamName = $"{entityType.GetEventStoreName()}_{identity.ToString("N").ToLower()}";
	        return string.IsNullOrEmpty(category) ? streamName : category + "-" + streamName;
        }

        public static string GetEventStoreName(this Type type)
        {
            return type.FullName.Replace('.', '_').Replace('+', '_').ToLower();
        }

		// use full clr name, and deserialize using it, instead of relying on the subscriber handling types
		public static string[] GetEventTopics(this Type eventType)
        {
	        return eventType
		        .GetInterfaces()
		        .Select(x => x.GetEventStoreName())
				.ToArray();
        }
    }
}
