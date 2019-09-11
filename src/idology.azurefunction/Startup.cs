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
            Func<string, ILogger, CreateEventReceiver> createEventReceiverFactory = 
                new CreateEventReceiverFactoryService(eventStoreConnectionUri, x => x).CreateEventReceiverFactory;
            Func<ILogger, Task<IEventStoreConnection>> createEventStoreConnection =
                new CreateEventStoreConnectionService(eventStoreConnectionUri, x => x).CreateEventStoreConnection;

            builder.AddDependencyInjection();

            /*
            builder.Services.AddSingleton<CreateEventStoreConnection>(
                createEventStoreConnection
                    .Memoize(new MemoryCache(new MemoryCacheOptions()), new MemoryCacheEntryOptions { Priority = CacheItemPriority.NeverRemove }, x => 0)
                    .Invoke);
            builder.Services.AddSingleton<CreateEventReceiverFactory>(
                createEventReceiverFactory
                    .Memoize(new MemoryCache(new MemoryCacheOptions()), new MemoryCacheEntryOptions{ Priority = CacheItemPriority.NeverRemove }, x => x.Item1)
                    .Invoke);
            */

            builder.Services.AddSingleton<SendCommand>(
                new SendCommandService(
                    createEventStoreConnection
                        .Memoize(new MemoryCache(new MemoryCacheOptions()), new MemoryCacheEntryOptions { Priority = CacheItemPriority.NeverRemove }, x => 0)
                        .Invoke,
                    createEventReceiverFactory
                        .Memoize(new MemoryCache(new MemoryCacheOptions()), new MemoryCacheEntryOptions { Priority = CacheItemPriority.NeverRemove }, x => x.Item1)
                        .Invoke)
                    .SendCommand);
        }
    }
}

public struct Command
{
    public Command(Guid commandId, string commandName, byte[] commandData)
    {
        CommandId = commandId;
        CommandName = commandName;
        CommandData = commandData;
    }

    public Guid CommandId { get; }
    public string CommandName { get; }
    public byte[] CommandData { get; }
}
public delegate Task<HttpResponseMessage> SendCommand(Guid correlationId, Command command, string[] commandCompletionMessageTypes, ILogger logger, CancellationTokenSource cts, Uri resultBaseUri, Uri queueBaseUri, Uri callbackUri = null);

public delegate Task<IEventStoreConnection> CreateEventStoreConnection(ILogger logger);
public delegate Task<ISourceBlock<ResolvedEvent>> CreateEventReceiver(Predicate<ResolvedEvent> eventFilter);
public delegate CreateEventReceiver CreateEventReceiverFactory(string streamName, ILogger logger);
