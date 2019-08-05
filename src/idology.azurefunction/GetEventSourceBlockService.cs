using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using eventstore;
using EventStore.ClientAPI;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace idology.azurefunction
{
    public class GetEventSourceBlockService
    {
        private readonly Func<ILogger, EventStoreObjectName, Task<ISourceBlock<ResolvedEvent>>> _getEventsSourceBlock;

        public GetEventSourceBlockService(Func<ILogger, EventStoreObjectName, Task<ISourceBlock<ResolvedEvent>>> getEventsSourceBlock)
        {
            _getEventsSourceBlock = getEventsSourceBlock;
        }

        public async Task<ISourceBlock<ResolvedEvent>> GetEventSourceBlock(ILogger logger, EventStoreObjectName sourceStreamName, Predicate<ResolvedEvent> eventFilter)
        {
            var eventsSourceBlock = await _getEventsSourceBlock(logger, sourceStreamName);
            var wob = new WriteOnceBlock<ResolvedEvent>(x => x);
            eventsSourceBlock.LinkTo(wob, new DataflowLinkOptions { MaxMessages = 1 }, eventFilter);
            return wob;
        }
    }
}
