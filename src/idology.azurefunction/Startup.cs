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

            var eventStoreConnectionUri =
                new Uri(
                    $"tcp://{EventStoreSettings.Username}:{EventStoreSettings.Password}@{EventStoreSettings.ClusterDns}:2112");
            builder.Services.AddSingleton<IEventStoreConnectionProvider>(new EventStoreConnectionProvider(eventStoreConnectionUri, x => x));
            builder.Services.AddSingleton<IEventSourceBlockFactory>(new EventSourceBlockFactory(eventStoreConnectionUri, x => x, "ce-message", "$ce-message"));
        }
    }
}
