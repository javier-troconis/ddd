using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace shared
{
    public interface IRecordedEvent<out TEvent>
    {
		int EventNumber { get; }
	    Guid EventId { get; }
	    DateTime Created { get; }
	    TEvent Event { get; }
    }
}
