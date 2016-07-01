using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Exceptions;
using shared;

namespace infra
{
    public delegate bool TryResolveConflict(IEnumerable<IEvent> changes, IEnumerable<IEvent> changesSinceLastWrite, out IEnumerable<IEvent> mergedChanges);

    public static class ConflictResolutionType
    {
        public static readonly TryResolveConflict IgnoreChangesSinceLastWrite = delegate (IEnumerable<IEvent> changes, IEnumerable<IEvent> changesSinceLastWrite, out IEnumerable<IEvent> mergedChanges)
        {
            mergedChanges = changes;
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
                if (!tryResolveConflict(events, changesSinceLastWrite, out events))
                {
                    throw new ConcurrencyException();
                }
            }
        }
    }
}
