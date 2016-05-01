using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using shared;

namespace infra
{
    public static class ConcurrentEventStoreWriter
    {
        //public static async Task<WriteResult> WriteEventsAsync(IEventStore eventStore, string streamName, int streamExpectedVersion, IEnumerable<IEvent> events, Action<IDictionary<string, object>> configureEventHeader = null)
        //{
        //    try
        //    {
        //        await eventStore.WriteEventsAsync(streamName, streamExpectedVersion, events, configureEventHeader);
        //    }
        //    catch()
        //    {

        //    }
        //}
    }
}
