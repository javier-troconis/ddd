using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eventstore
{
    public interface IRecordedEvent<out TData>
    {
        string OriginalStreamId { get; }
        long OriginalEventNumber { get; }
        string EventStreamId { get; }
        long EventNumber { get; }
        Guid EventId { get; }
        DateTime Created { get; }
        TData Data { get; }
    }
}
