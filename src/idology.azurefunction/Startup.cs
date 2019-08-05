using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using azurefunction;
using eventstore;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using idology.azurefunction;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using shared;
using ILogger = Microsoft.Extensions.Logging.ILogger;

[assembly: WebJobsStartup(typeof(Startup))]
namespace idology.azurefunction
{
    public class Startup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.AddDependencyInjection();

            var eventStoreConnectionUri =
                new Uri(
                    $"tcp://{EventStoreSettings.Username}:{EventStoreSettings.Password}@{EventStoreSettings.ClusterDns}:2112");

            var memoryCache = new MemoryCache(new MemoryCacheOptions());

            var getEventStoreConnectionService = new GetEventStoreConnectionService(eventStoreConnectionUri, x => x);
            var getEventStoreConnection = new Func<ILogger, Task<IEventStoreConnection>>(getEventStoreConnectionService.GetEventStoreConnection)
                .Memoize(memoryCache, new MemoryCacheEntryOptions { Priority = CacheItemPriority.NeverRemove });
            builder.Services.AddSingleton(getEventStoreConnection);

            var getEventsSourceBlockService = new GetEventsSourceBlockService(eventStoreConnectionUri, x => x);
            var getEventsSourceBlock = new Func<EventStoreObjectName, ILogger, Task<ISourceBlock<ResolvedEvent>>>(getEventsSourceBlockService.GetEventsSourceBlock)
                .Memoize(memoryCache, new MemoryCacheEntryOptions { Priority = CacheItemPriority.NeverRemove });
            var getEventSourceBlockService = new GetEventSourceBlockService(getEventsSourceBlock);
            var getEventSourceBlock =
                new Func<EventStoreObjectName, ILogger, Predicate<ResolvedEvent>, Task<ISourceBlock<ResolvedEvent>>>(
                    getEventSourceBlockService.GetEventSourceBlock);
            builder.Services.AddSingleton(getEventSourceBlock);



          
            /*
            var getEventReceiverFactory =
                new Func<ILogger, EventStoreObjectName,
                        Task<ISourceBlock<ResolvedEvent>>>(
                        new GetEventReceiverFactoryService(eventStoreUri, x => x).GetEventReceiverFactory)
                    .Tuplify()
                    .Memoize(memoryCache, new MemoryCacheEntryOptions {Priority = CacheItemPriority.NeverRemove});
            var f = new Func<ISourceBlock<ResolvedEvent>, Predicate<ResolvedEvent>, ISourceBlock<ResolvedEvent>>(
                GetEventReceiverFactoryService.GetEventSourceBlock).Curry();
            getEventReceiverFactory.ComposeForward(f)

            builder.Services.AddSingleton<Func<ILogger, EventStoreObjectName, Predicate<ResolvedEvent>, Task<ISourceBlock<ResolvedEvent>>>>
                (
                    (logger, sourceStreamName, eventFilter) => getEventReceiverFactory(logger, sourceStreamName)(eventFilter)
                );
            



            var n = new Func<ILogger, EventStoreObjectName, Task<ISourceBlock<ResolvedEvent>>>(
                    new GetEventReceiverFactoryService(eventStoreUri, x => x).GetStreamSourceBlock)
                .Tuplify()
                .Memoize(memoryCache, new MemoryCacheEntryOptions {Priority = CacheItemPriority.NeverRemove});
*/






            //builder.Services.AddSingleton<Func<ILogger, Task<IEventStoreConnection>>>(new GetEventStoreConnectionService(
            //    new Uri($"tcp://{EventStoreSettings.Username}:{EventStoreSettings.Password}@{EventStoreSettings.ClusterDns}:2112"), x => x));
            //builder.Services.AddSingleton<IEventReceiverFactory>(new GetEventSourceBlockService(
            //    eventStoreUri, x => x, "et-verifyidentitysucceeded", "$et-verifyidentitysucceeded"));
        }
    }
}
