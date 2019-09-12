using System;
using System.ComponentModel;
using System.Net.Http;
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
using ILogger = Microsoft.Extensions.Logging.ILogger;
using shared;

[assembly: WebJobsStartup(typeof(Startup))]
namespace idology.azurefunction
{
	public class Startup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            var eventStoreConnectionUri =
                new Uri(
                    $"tcp://{EventStoreSettings.Username}:{EventStoreSettings.Password}@{EventStoreSettings.ClusterDns}:2112");

            Func<ILogger, Task<IEventStoreConnection>> createEventStoreConnection = 
                new CreateEventStoreConnectionService(eventStoreConnectionUri, x => x).CreateEventStoreConnection;

            Func<EventStoreObjectName, ILogger, CreateEventReceiver> createEventReceiverFactory = 
                new CreateEventReceiverFactoryService(eventStoreConnectionUri, x => x).CreateEventReceiverFactory;

            var createEventReceiverFactoryCachedByStreamName = createEventReceiverFactory.Memoize(
                new MemoryCache(new MemoryCacheOptions()),
                new MemoryCacheEntryOptions {Priority = CacheItemPriority.NeverRemove}, x => x.Item1);

            var createEventStoreConnectionCachedByProcessInstance = createEventStoreConnection.Memoize(
                new MemoryCache(new MemoryCacheOptions()),
                new MemoryCacheEntryOptions {Priority = CacheItemPriority.NeverRemove}, x => Unit.Value);

            builder.AddDependencyInjection();
            builder.Services.AddSingleton<ISendCommandService>(new EventStoreSendCommandService(createEventReceiverFactoryCachedByStreamName, createEventStoreConnectionCachedByProcessInstance));
            builder.Services.AddSingleton<IReadMessagesService<ResolvedEvent>>(new EventStoreReadMessagesService(createEventStoreConnectionCachedByProcessInstance));
        }
    }
}



