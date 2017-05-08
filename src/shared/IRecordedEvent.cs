using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace shared
{
    public interface IRecordedEvent<TEvent>
    {
		long OriginalEventNumber { get; }
	    string EventStreamId { get; }
	    long EventNumber { get; }
	    Guid EventId { get; }
	    DateTime Created { get; }
	    TEvent Event { get; }
    }
}
