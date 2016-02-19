using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace es
{
    public static class EventStore
    {
		static readonly Dictionary<Guid, List<IEvent>> Events = new Dictionary<Guid, List<IEvent>>(); 

		public static IEnumerable<IEvent> GetEvents(Guid streamId)
		{
			return Events[streamId];
		}

		public static void SaveEvents(Guid streamId, int expectedVersion, IEnumerable<IEvent> events)
		{
			if(!Events.ContainsKey(streamId))
			{
				Events[streamId] = new List<IEvent>();
			}
			Events[streamId].AddRange(events);
		}
    }
}
