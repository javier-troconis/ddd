using System;

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
		TData Data { get; }
    }
}
