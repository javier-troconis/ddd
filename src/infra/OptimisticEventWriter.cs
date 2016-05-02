using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Exceptions;
using shared;

namespace infra
{
    public delegate bool CanCommitWhenNewerStreamVersionIsFound(IEnumerable<IEvent> eventsToWrite, IEnumerable<IEvent> eventsSinceLastWrite);

    public static class OptimisticEventWriter
    {
        public static readonly CanCommitWhenNewerStreamVersionIsFound AlwaysCommit = delegate { return true; };

        public static async Task<WriteResult> WriteEventsAsync(CanCommitWhenNewerStreamVersionIsFound canCommitWhenNewerStreamVersionIsFound, IEventStore eventStore, string streamName, int streamExpectedVersion, 
            IEnumerable<IEvent> events, IDictionary<string, object> eventHeader = null)
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
                    throw new Exception("Invalid stream version");
                }
                if (!canCommitWhenNewerStreamVersionIsFound(events, eventsSinceLastWrite))
                {
                    throw new Exception("Commit conflict could not be resolved");
                }
            }
        }
    }
}
