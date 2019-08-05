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
            [Dependency(typeof(IEventStoreConnectionProvider))] IEventStoreConnectionProvider eventStoreConnectionProvider,
            [Dependency(typeof(IEventSourceBlockFactory))] IEventSourceBlockFactory eventSourceBlockFactory, 
	        ILogger logger)
	    {
            var ct1 = new CancellationTokenSource(500);
            var correlationId = ctx.InvocationId.ToString();
           
            var eventSourceBlock = await eventSourceBlockFactory.CreateEventSourceBlock(logger,
                x => Equals(x.Event.EventType, "verifyidentitysucceeded") && 
                        Equals(x.Event.Metadata.ParseJson<IDictionary<string, string>>()[EventHeaderKey.CorrelationId], correlationId));

            var eventStoreConnection = await eventStoreConnectionProvider.ProvideEventStoreConnection(logger);
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

	        try
	        {
	            var @event = await eventSourceBlock.ReceiveAsync(ct1.Token);
	            var response1 = new HttpResponseMessage(HttpStatusCode.OK);
                response1.Headers.Location = new Uri($"http://localhost:7071/x/{correlationId}");
	            response1.Content = new StringContent(new
	                {
	                    commandCorrelationId = correlationId,
	                    eventCorrelationId =
	                        @event.Event.Metadata.ParseJson<IDictionary<string, string>>()[
	                            EventHeaderKey.CorrelationId]
	                }.ToJson(),
	                Encoding.UTF8, "application/json"
	            );
                return response1;
            }
	        catch (TaskCanceledException)
	        {
               
	            var response2 = new HttpResponseMessage(HttpStatusCode.Accepted);
                response2.Headers.Location = new Uri($"http://localhost:7071/taskqueue/{correlationId}");
	            return response2;
	        }
	    }


        [FunctionName(nameof(GetTaskQueue))]
        public static async Task<HttpResponseMessage> GetTaskQueue(
           CancellationToken ct,
           [HttpTrigger(AuthorizationLevel.Function, "get", Route = "taskqueue/{taskqueueid}")] HttpRequestMessage request,
           string taskqueueid,
           ExecutionContext ctx,
           [Dependency(typeof(IEventStoreConnectionProvider))] IEventStoreConnectionProvider eventStoreConnectionProvider,
           ILogger logger)
        {
            var eventStoreConnection = await eventStoreConnectionProvider.ProvideEventStoreConnection(logger);
            var events = eventStoreConnection.ReadStreamEventsForwardAsync($"$bc-{taskqueueid}", 0, int.MaxValue, true,
                new UserCredentials(EventStoreSettings.Username, EventStoreSettings.Password));
            return null;
        }
    }

}



