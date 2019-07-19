using System;
using System.Threading.Tasks;
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

            var deferredEventPipeline = new Lazy<Task<Func<ResolvedEvent, Task<ResolvedEvent>>>>( 
                    async () =>
                    {
                        Func<ResolvedEvent, Task<ResolvedEvent>> pipeline = Task.FromResult;
                        var eventBus = EventBus.CreateEventBus
                        (
                            () => new EventStoreConnectionFactory(
                                    EventStoreSettings.ClusterDns,
                                    EventStoreSettings.InternalHttpPort,
                                    EventStoreSettings.Username,
                                    EventStoreSettings.Password,
                                    x => x.WithConnectionTimeoutOf(TimeSpan.FromMinutes(1)))
                                .CreateConnection(),
                            registry => registry.RegisterVolatileSubscriber("subscriberName", "subscriptionStreamName", pipeline)
                        );
                        await eventBus.StartAllSubscribers();
                        return pipeline;
                    });
            builder.Services.AddSingleton(deferredEventPipeline);
        }
    }
}
