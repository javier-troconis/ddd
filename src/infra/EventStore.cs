using System;
using System.Collections.Generic;
using shared;

namespace ConsoleApp3
{
    public static class EventStore
    {
		static readonly Dictionary<Guid, List<Event>> Events = new Dictionary<Guid, List<Event>>(); 

		public static IEnumerable<Event> GetEvents(Guid streamId)
		{
			return Events[streamId];
		}

		public static void SaveEvents(Guid streamId, int expectedVersion, IEnumerable<Event> events)
		{
			if(!Events.ContainsKey(streamId))
			{
				Events[streamId] = new List<Event>();
			}
			Events[streamId].AddRange(events);
		}
    }
}
