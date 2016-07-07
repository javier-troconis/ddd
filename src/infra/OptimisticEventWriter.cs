using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Exceptions;
using shared;

namespace infra
{
    public delegate bool TryResolveEventConflict(IEnumerable<IEvent> newChanges, IEnumerable<IEvent> conflictingChanges, out IEnumerable<IEvent> mergedChanges);

    public static class EventConflictResolutionStrategy
    {
        public static readonly TryResolveEventConflict IgnoreConflictingChanges = delegate (IEnumerable<IEvent> newChanges, IEnumerable<IEvent> conflictingChanges, out IEnumerable<IEvent> mergedChanges)
        {
            mergedChanges = newChanges;
            return true;
        };
    }

    public static class OptimisticEventWriter
    {
        public static async Task<WriteResult> WriteEventsAsync(TryResolveEventConflict writeConflicted, IEventStore eventStore, 
            string streamName, int streamExpectedVersion, IEnumerable<IEvent> events, IDictionary<string, object> eventHeader = null)
        {
            while (true)
            {
                IEnumerable<IEvent> changesSinceLastWrite;
                try
                {
                    return await eventStore.WriteEventsAsync(streamName, streamExpectedVersion, events, eventHeader);
                }
                catch (WrongExpectedVersionException)
                {
                    changesSinceLastWrite = await eventStore.ReadEventsAsync(streamName, streamExpectedVersion + 1);
                    streamExpectedVersion = streamExpectedVersion + changesSinceLastWrite.Count();
                }
                if (!changesSinceLastWrite.Any())
                {
                    throw new Exception($"Non existent version {streamExpectedVersion} for stream {streamName}");
                }
                if (!writeConflicted(events, changesSinceLastWrite, out events))
                {
                    throw new ConcurrencyException();
                }
            }
        }
    }
}
