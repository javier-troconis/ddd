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
    public delegate Task<ISourceBlock<ResolvedEvent>> GetEventSourceBlock(ILogger logger, EventStoreObjectName sourceStreamName, Predicate<ResolvedEvent> eventFilter);
}
