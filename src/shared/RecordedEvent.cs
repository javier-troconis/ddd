using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace shared
{
    public struct RecordedEvent<TEvent>
    {
		public readonly Guid  EventId;
	    public readonly int EventNumber;
	    public readonly DateTime Created;
	    public readonly TEvent @Event;

	    public RecordedEvent(Guid eventId, int eventNumber, DateTime created, TEvent @event)
	    {
		    EventId = eventId;
		    EventNumber = eventNumber;
		    Created = created;
		    Event = @event;
	    }
    }
}
