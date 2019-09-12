using System;
using System.Collections.Generic;
using System.Text;
using EventStore.ClientAPI;

namespace eventstore
{
    public static class RecordedEventExtensions
    {
        public static bool TryGetCorrelationId(this RecordedEvent recordedEvent, out string correlationId)
        {
            if (recordedEvent.Metadata.ParseJson<IDictionary<string, object>>()
                .TryGetValue(EventHeaderKey.CorrelationId, out var value))
            {
                correlationId = value.ToString();
                return true;
            }
            correlationId = default(string);
            return false;
        }

        public static bool TryGetCausationId(this RecordedEvent recordedEvent, out string causationId)
        {
            if (recordedEvent.Metadata.ParseJson<IDictionary<string, object>>()
                .TryGetValue(EventHeaderKey.CausationId, out var value))
            {
                causationId = value.ToString();
                return true;
            }
            causationId = default(string);
            return false;
        }
    }
}
