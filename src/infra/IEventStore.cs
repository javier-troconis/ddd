using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using shared;

namespace infra
{
    public interface IEventStore
    {
		Task<IReadOnlyList<Event>> GetEventsAsync(string streamName);
		Task SaveEventsAsync(string streamName, int expectedVersion, IEnumerable<Event> events);
	}
}
