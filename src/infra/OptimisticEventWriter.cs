using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Exceptions;
using shared;

namespace infra
{
    public delegate bool TryResolveConflict(IEnumerable<object> newChanges, IEnumerable<object> conflictingChanges, out IEnumerable<object> mergedChanges);

    public static class ConflictResolutionStrategy
    {
        public static readonly TryResolveConflict IgnoreConflictingChanges = delegate (IEnumerable<object> newChanges, IEnumerable<object> conflictingChanges, out IEnumerable<object> mergedChanges)
        {
            mergedChanges = newChanges;
            return true;
        };
    }

    public static class OptimisticEventWriter
    {
        public static async Task<WriteResult> WriteEventsAsync(TryResolveConflict tryResolveConflict, IEventStore eventStore, string streamName, int streamExpectedVersion, 
            IEnumerable<object> events, Action<IDictionary<string, object>> beforeSavingEvent = null)
        {
            while (true)
            {
                try
                {
                    return await eventStore.WriteEventsAsync(streamName, streamExpectedVersion, events, beforeSavingEvent);
                }
                catch (WrongExpectedVersionException)
                {
                    var nextStreamVersion = streamExpectedVersion + 1;
                    var eventsSinceLastWrite = await eventStore.ReadEventsForwardAsync(streamName, nextStreamVersion);
                     if (!eventsSinceLastWrite.Any())
                    {
                        throw new Exception($"stream {streamName} is not at version {nextStreamVersion}");
                    }
                    if (!tryResolveConflict(events, eventsSinceLastWrite, out events))
                    {
                        throw new StreamConflictException();
                    }
                    streamExpectedVersion += eventsSinceLastWrite.Count;
                } 
            }
        }
    }
}
