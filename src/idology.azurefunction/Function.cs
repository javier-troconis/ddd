using System;
using System.Collections.Generic;
using System.Linq;
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
using Newtonsoft.Json.Linq;
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
            [Dependency(typeof(IEventSourceBlockFactory))] IEventSourceBlockFactory eventSourceBlockFactory, 
	        ILogger logger)
	    {
            var ct1 = new CancellationTokenSource(500);
            var correlationId = ctx.InvocationId.ToString();
           
            var eventSourceBlock = await eventSourceBlockFactory.CreateEventSourceBlock(logger,
                x => Equals(x.Event.EventType, "verifyidentitysucceeded") && 
                        Equals(x.Event.Metadata.ParseJson<IDictionary<string, string>>()[EventHeaderKey.CorrelationId], correlationId));

	        var body = await request.Content.ReadAsStringAsync();
            var eventStoreConnection = await eventStoreConnectionProvider.ProvideEventStoreConnection(logger);
            await eventStoreConnection.AppendToStreamAsync($"message-{Guid.NewGuid()}", ExpectedVersion.NoStream,
                new[]
                {
                    new EventData(Guid.NewGuid(), "verifyidentity", false, body.ToJsonBytes(),
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
	            response1.Content = new StringContent(
	                new Dictionary<string, string>
	                {
	                    {
                            "commandCorrelationId", correlationId
                        },
	                    {
                            "eventCorrelationId",
                                    @event.Event.Metadata.ParseJson<IDictionary<string, string>>()[
                                        EventHeaderKey.CorrelationId]
                        }
	                }.ToJson(),
                    Encoding.UTF8, "application/json"
	            );
                return response1;
            }
	        catch (TaskCanceledException)
	        {
	            await eventStoreConnection.AppendToStreamAsync($"message-{Guid.NewGuid()}", ExpectedVersion.NoStream,
	                new[]
	                {
	                    new EventData(Guid.NewGuid(), "tasktimedout", false,
	                        new Dictionary<string, object>
	                        {
	                            {
                                    "taskName", "verifyidentity"
	                            },
                                {
	                                "completionEventTypes", new []{ "verifyidentitysucceeded" }
	                            },
	                            {
	                                "callbackUri", string.Empty
	                            },
	                            {
	                                "resultUri", $"http://localhost:7071/x/{correlationId}"
	                            }
                            }.ToJsonBytes(),
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
                var response2 = new HttpResponseMessage(HttpStatusCode.Accepted);
                response2.Headers.Location = new Uri($"http://localhost:7071/queue/{correlationId}");
	            return response2;
	        }
	    }

        

        [FunctionName(nameof(GetQueue))]
        public static async Task<HttpResponseMessage> GetQueue(
           CancellationToken ct,
           [HttpTrigger(AuthorizationLevel.Function, "get", Route = "queue/{correlationId}")] HttpRequestMessage request,
           string correlationId,
           ExecutionContext ctx,
           [Dependency(typeof(IEventStoreConnectionProvider))] IEventStoreConnectionProvider eventStoreConnectionProvider,
           ILogger logger)
        {
            var eventStoreConnection = await eventStoreConnectionProvider.ProvideEventStoreConnection(logger);
            var events = await eventStoreConnection.ReadStreamEventsForwardAsync($"$bc-{correlationId}", 0, 4096, true,
                new UserCredentials(EventStoreSettings.Username, EventStoreSettings.Password));
            var eventByEventType = events.Events.ToDictionary(x => x.Event.EventType, x => x.Event);

            if (!eventByEventType.TryGetValue("tasktimedout", out var recordedEvent))
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
            var data = recordedEvent.Data.ParseJson<IDictionary<string, object>>();
            var completionEventTypes = ((JArray) data["completionEventTypes"]).ToObject<string[]>();
            var isCompleted = eventByEventType.Select(x => x.Key).Any(completionEventTypes.Contains);
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Headers.Location = isCompleted ? new Uri((string)data["resultUri"]) : null;
            return response;
        }
    }

}



