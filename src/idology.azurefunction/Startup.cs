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

            var eventStoreUri =
                new Uri(
                    $"tcp://{EventStoreSettings.Username}:{EventStoreSettings.Password}@{EventStoreSettings.ClusterDns}:2112");
            var memoryCache = new MemoryCache(new MemoryCacheOptions());

            builder.Services.AddSingleton
                (
                    new Func<ILogger, Task<IEventStoreConnection>>(
                        new GetEventStoreConnectionService(eventStoreUri, x => x).GetEventStoreConnection)
                            .Memoize(memoryCache, new MemoryCacheEntryOptions{Priority = CacheItemPriority.NeverRemove})
                );
            //builder.Services.AddSingleton<Func<ILogger, Task<IEventStoreConnection>>>(new GetEventStoreConnectionService(
            //    new Uri($"tcp://{EventStoreSettings.Username}:{EventStoreSettings.Password}@{EventStoreSettings.ClusterDns}:2112"), x => x));
            builder.Services.AddSingleton<IEventReceiverFactory>(new GetEventSourceBlockService(
                eventStoreUri, x => x, "et-verifyidentitysucceeded", "$et-verifyidentitysucceeded"));
        }
    }
}
