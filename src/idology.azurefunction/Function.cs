using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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
using Microsoft.AspNetCore.Mvc.Formatters;
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
	        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "x/{callbackuri?}")] HttpRequestMessage request, 
	        // this binding doesn't work
	        string callbackuri,
            ExecutionContext ctx,
            [Dependency(typeof(GetEventStoreConnection))] GetEventStoreConnection getEventStoreConnection,
            [Dependency(typeof(GetEventSourceBlock))] GetEventSourceBlock getEventSourceBlock, 
	        ILogger logger)
	    {
            var correlationId = ctx.InvocationId.ToString();
           
            var eventSourceBlock = await getEventSourceBlock(logger, "$et-verifyidentitysucceeded", x => Equals(x.Event.Metadata.ParseJson<IDictionary<string, string>>()[EventHeaderKey.CorrelationId], correlationId));

            var eventStoreConnection = await getEventStoreConnection(logger);
            await eventStoreConnection.AppendToStreamAsync($"message-{Guid.NewGuid():N}", ExpectedVersion.NoStream,
                new[]
                {
                    new EventData(Guid.NewGuid(), "verifyidentity", false, new byte[0],
                        new Dictionary<string, string>
                        {
                            {
                                EventHeaderKey.CorrelationId, correlationId
                            }
                        }.ToJsonBytes()
                    )
                },
                new UserCredentials(EventStoreSettings.Username, EventStoreSettings.Password)
            );

            var result = await eventSourceBlock.ReceiveAsync(ct);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content =
                    new StringContent(new
                    {
                        commandCorrelationId = correlationId,
                        eventCorrelationId = result.Event.Metadata.ParseJson<IDictionary<string, string>>()[EventHeaderKey.CorrelationId]
                    }.ToJson(),
                        Encoding.UTF8, "application/json"
                    )
            };
	    }
    }
}
