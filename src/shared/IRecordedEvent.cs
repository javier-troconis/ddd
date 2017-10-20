using System;
using System.Collections.Generic;

namespace shared
{
    public interface IRecordedEvent<out TData>
    {
		string OriginalStreamId { get; }
	    long OriginalEventNumber { get; }
	    string EventStreamId { get; }
	    long EventNumber { get; }
	    DateTime Created { get; }
	    Guid EventId { get; }
	    Guid? CorrelationId { get; }
	    IReadOnlyDictionary<string, object> Metadata { get; }
		TData Data { get; }
    }
}
