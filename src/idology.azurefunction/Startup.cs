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
            GetEventStoreConnection getEventStoreConnection = new Func<ILogger, Task<IEventStoreConnection>>(getEventStoreConnectionService.GetEventStoreConnection)
                .Memoize(memoryCache, new MemoryCacheEntryOptions { Priority = CacheItemPriority.NeverRemove })
                .Invoke;
            builder.Services.AddSingleton(getEventStoreConnection);

            var getEventsSourceBlockService = new GetEventsSourceBlockService(eventStoreConnectionUri, x => x);
            var getEventsSourceBlock = new Func<ILogger, EventStoreObjectName, Task<ISourceBlock<ResolvedEvent>>>(getEventsSourceBlockService.GetEventsSourceBlock)
                .Memoize(memoryCache, new MemoryCacheEntryOptions { Priority = CacheItemPriority.NeverRemove });
            var getEventSourceBlockService = new GetEventSourceBlockService(getEventsSourceBlock);
            GetEventSourceBlock getEventSourceBlock =
                new Func<ILogger, EventStoreObjectName, Predicate<ResolvedEvent>, Task<ISourceBlock<ResolvedEvent>>>(
                    getEventSourceBlockService.GetEventSourceBlock)
                    .Invoke;
            builder.Services.AddSingleton(getEventSourceBlock);
        }
    }
}
