using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using azurefunction;
using eventstore;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using shared;

namespace idology.azurefunction
{
	public static class Function
	{
	    [FunctionName(nameof(EvaluateOfacCompliance))]
	    public static async Task<HttpResponseMessage> EvaluateOfacCompliance(
	        CancellationToken ct, 
	        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "x")] HttpRequestMessage request, 
	        ExecutionContext ctx,
	        [Dependency(typeof(IEventStoreConnectionProvider))] IEventStoreConnectionProvider eventStoreConnectionProvider,
            [Dependency(typeof(IEventReceiverFactory))] IEventReceiverFactory eventReceiverFactory, 
	        ILogger logger)
	    {
	        var eventId = Guid.NewGuid();
	        var eventReceiver = await eventReceiverFactory.CreateEventReceiver(logger, x => Equals(x.Event.EventId, eventId));
	        var eventStoreConnection = await eventStoreConnectionProvider.ProvideEventStoreConnection(logger);
	        await eventStoreConnection.AppendToStreamAsync($"identityverification-{Guid.NewGuid():N}", ExpectedVersion.NoStream, new[]{new EventData(eventId, "verifyidentity", false, new byte[0], new byte[0])}, new UserCredentials(EventStoreSettings.Username, EventStoreSettings.Password));
	        var result = await eventReceiver.Receive(ct);
            return new HttpResponseMessage(HttpStatusCode.OK){Content = new StringContent(result.Event.EventId.ToString())};
	    }
    }
}
