using System;
using System.Collections.Generic;
using System.Text;
using EventStore.ClientAPI;

namespace eventstore
{
    public static class RecordedEventExtensions
    {
        public static bool TryGetCorrelationId(this RecordedEvent recordedEvent, out object correlationId)
        {
            return recordedEvent.Metadata.ParseJson<IDictionary<string, object>>().TryGetValue(EventHeaderKey.CorrelationId, out correlationId);
        }

        public static bool TryGetCausationId(this RecordedEvent recordedEvent, out object causationId)
        {
            return recordedEvent.Metadata.ParseJson<IDictionary<string, object>>().TryGetValue(EventHeaderKey.CausationId, out causationId);
        }
    }
}
