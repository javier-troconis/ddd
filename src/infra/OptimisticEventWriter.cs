using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Exceptions;
using shared;

namespace infra
{
    public delegate bool TryResolveConflict(IEnumerable<IEvent> newChanges, IEnumerable<IEvent> conflictingChanges, out IEnumerable<IEvent> mergedChanges);

    public static class ConflictResolutionStrategy
    {
        public static readonly TryResolveConflict IgnoreConflictingChanges = delegate (IEnumerable<IEvent> newChanges, IEnumerable<IEvent> conflictingChanges, out IEnumerable<IEvent> mergedChanges)
        {
            mergedChanges = newChanges;
            return true;
        };
    }

    public static class OptimisticEventWriter
    {
        public static async Task<WriteResult> WriteEventsAsync(TryResolveConflict tryResolveConflict, IEventStore eventStore, 
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
                    throw new Exception($"Non existent version {streamExpectedVersion} for stream {streamName}");
                }
                if (!tryResolveConflict(events, eventsSinceLastWrite, out events))
                {
                    throw new ConcurrencyException();
                }
            }
        }
    }
}
