using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using azurefunction;
using eventstore;
using EventStore.ClientAPI;
using idology.azurefunction;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.DependencyInjection;
using shared;

[assembly: WebJobsStartup(typeof(Startup))]
namespace idology.azurefunction
{
	public class Startup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.AddDependencyInjection();

            var deferredEventStoreConnection = new Lazy<Task<IEventStoreConnection>>(async () =>
            {
                Func<IEventStoreConnection> createConnection = new EventStoreConnectionFactory(
                        EventStoreSettings.ClusterDns,
                        EventStoreSettings.InternalHttpPort,
                        EventStoreSettings.Username,
                        EventStoreSettings.Password,
                        x => x.WithConnectionTimeoutOf(TimeSpan.FromMinutes(1)))
                    .CreateConnection;
                var connection = createConnection();
                await connection.ConnectAsync();
                return connection;
            });
            builder.Services.AddSingleton(deferredEventStoreConnection);

            var deferredCreateReceiveEvent = new Lazy<Task<Func<Predicate<ResolvedEvent>, Func<CancellationToken, Task<ResolvedEvent>>>>>( 
                    async () =>
                    {
                        var bb = new BroadcastBlock<ResolvedEvent>(x => x);
                        var eventBus = EventBus.CreateEventBus
                        (
                            () => new EventStoreConnectionFactory(
                                    EventStoreSettings.ClusterDns,
                                    EventStoreSettings.InternalHttpPort,
                                    EventStoreSettings.Username,
                                    EventStoreSettings.Password,
                                    x => x.WithConnectionTimeoutOf(TimeSpan.FromMinutes(1)))
                                .CreateConnection(),
                            registry => registry.RegisterVolatileSubscriber("subscriberName", "subscriptionStreamName", bb.SendAsync)
                        );
                        await eventBus.StartAllSubscribers();
                        return filter =>
                        {
                            var wob = new WriteOnceBlock<ResolvedEvent>(x => x);
                            bb.LinkTo(wob, new DataflowLinkOptions { MaxMessages = 1 }, filter);
                            return wob.ReceiveAsync;
                        };
                    });
            builder.Services.AddSingleton(deferredCreateReceiveEvent);
        }
    }
}
