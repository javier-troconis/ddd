using System;
using System.Collections.Generic;

namespace shared
{
    public interface IRecordedEvent<out TBody>
    {
	    IRecordedEventHeader Header { get; } 
		TBody Body { get; }
    }

	public interface IRecordedEventHeader : IReadOnlyDictionary<string, object>
	{
		string OriginalStreamId { get; }
		long OriginalEventNumber { get; }
		string EventStreamId { get; }
		long EventNumber { get; }
		DateTime Created { get; }
		Guid EventId { get; }
		Guid? CorrelationId { get; }
	}
}
