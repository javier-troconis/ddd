using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Exceptions;
using shared;

namespace infra
{
    public delegate IEnumerable<IEvent> ConflictResolutionStrategy(IEnumerable<IEvent> eventsToWrite, IEnumerable<IEvent> eventsSinceLastWrite);

    public static class StreamVersionConflictResolution
    {
        public static readonly ConflictResolutionStrategy AlwaysCommit = (eventsToWrite, eventsSinceLastWrite) => eventsToWrite;
    }

    public static class OptimisticEventWriter
    {
        public static async Task<WriteResult> WriteEventsAsync(ConflictResolutionStrategy conflictResolutionStrategy, IEventStore eventStore, 
            string streamName, int streamExpectedVersion, IEnumerable<IEvent> events, IDictionary<string, object> eventHeader = null)
        {
            while (true)
            {
                IEnumerable<IEvent> eventsSinceLastWrite;
                try
                {
                    return await eventStore.WriteEventsAsync(streamName, streamExpectedVersion, events, eventHeader);
                }
                catch (WrongExpectedVersionException)
                {
                    eventsSinceLastWrite = await eventStore.ReadEventsAsync(streamName, streamExpectedVersion + 1);
                    streamExpectedVersion = streamExpectedVersion + eventsSinceLastWrite.Count();
                }
                if (!eventsSinceLastWrite.Any())
                {
                    throw new Exception($"Version {streamExpectedVersion} does not exist for stream {streamName}");
                }
                events = conflictResolutionStrategy(events, eventsSinceLastWrite);
            }
        }
    }
}
