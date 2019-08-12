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
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using shared;

namespace idology.azurefunction
{
	public static class Function
	{
	    [FunctionName(nameof(VerifyIdentity))]
	    public static async Task<HttpResponseMessage> VerifyIdentity(
	        CancellationToken ct, 
	        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "identityverification")] HttpRequestMessage request, 
            ExecutionContext ctx,
            [Dependency(typeof(IGetEventStoreConnectionService))] IGetEventStoreConnectionService getEventStoreConnectionService,
            [Dependency(typeof(ICreateEventSourceBlockService))] ICreateEventSourceBlockService createEventSourceBlockService, 
	        ILogger logger)
	    {

	        var body = await request.Content.ReadAsStringAsync();
	        var data = body.ParseJson<Dictionary<string, string>>();
	        var requestTimeout = int.Parse(data["requestTimeout"]);
	        var resultCallbackUri = data["resultCallbackUri"];
            var ct1 = new CancellationTokenSource(requestTimeout);
            var correlationId = ctx.InvocationId.ToString();
            var eventSourceBlock = await createEventSourceBlockService.CreateEventSourceBlock(logger,
                x => Equals(x.Event.EventType, "verifyidentitysucceeded") && 
                     Equals(x.Event.Metadata.ParseJson<IDictionary<string, object>>()[EventHeaderKey.CorrelationId], correlationId));
            var eventStoreConnection = await getEventStoreConnectionService.GetEventStoreConnection(logger);
	        var eventId = Guid.NewGuid();
            await eventStoreConnection.AppendToStreamAsync($"message-{eventId}", ExpectedVersion.NoStream,
                new[]
                {
                    new EventData(eventId, "verifyidentity", false, new byte[0],
                        new Dictionary<string, object>
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
                var evaluateOfacComplianceResult = await eventSourceBlock.ReceiveAsync(ct1.Token);
	            var response1 = new HttpResponseMessage(HttpStatusCode.OK);
                response1.Headers.Location = new Uri($"http://localhost:7071/x/{evaluateOfacComplianceResult.Event.EventId}");
	            response1.Content = new StringContent(
	                new Dictionary<string, object>
	                {
	                    {
                            "cmdCorrelationId", correlationId
                        },
	                    {
                            "evtCorrelationId", evaluateOfacComplianceResult.Event.Metadata.ParseJson<IDictionary<string, object>>()[EventHeaderKey.CorrelationId]
                        }
	                }.ToJson(),
                    Encoding.UTF8, "application/json"
	            );
                return response1;
            }
	        catch (TaskCanceledException)
	        {
	            var operationTimeoutEventId = Guid.NewGuid();
	            using (var tx = await eventStoreConnection.StartTransactionAsync($"message-{operationTimeoutEventId}", ExpectedVersion.NoStream, new UserCredentials(EventStoreSettings.Username, EventStoreSettings.Password)))
	            {
	                await tx.WriteAsync
	                (
	                    new EventData(operationTimeoutEventId, "operationtimedout", false,
	                        new Dictionary<string, object>
	                        {
	                            {
	                                "operationName", "verifyidentity"
	                            },
	                            {
	                                "operationCompletionMessageTypes", new[] {"verifyidentitysucceeded"}
	                            },
                                {
                                    "baseResultUri", "http://localhost:7071/x"
	                            }
	                        }.ToJsonBytes(),
	                        new Dictionary<string, object>
	                        {
	                            {
	                                EventHeaderKey.CorrelationId, correlationId
	                            }
	                        }.ToJsonBytes()
	                    )
	                );
	                if (!string.IsNullOrEmpty(resultCallbackUri))
	                {
	                    await tx.WriteAsync
	                    (
	                        new EventData(Guid.NewGuid(), "callbackclient", false,
	                            new Dictionary<string, object>
	                            {
	                                {
                                        "clientUri", resultCallbackUri
                                    },
	                                {
	                                    "scriptId", Guid.NewGuid()
	                                },
	                                {
                                        "expectedVersion", ExpectedVersion.NoStream
	                                }
                                }.ToJsonBytes(),
	                            new Dictionary<string, object>
	                            {
	                                {
	                                    EventHeaderKey.CorrelationId, correlationId
	                                }
	                            }.ToJsonBytes()
	                        )
	                    );
                    }
	                await tx.CommitAsync();
	            }
                var response2 = new HttpResponseMessage(HttpStatusCode.Accepted);
                response2.Headers.Location = new Uri($"http://localhost:7071/queue/{operationTimeoutEventId}");
	            return response2;
	        }
	    }

        [FunctionName(nameof(GetVerifyIdentityResult))]
        public static async Task<HttpResponseMessage> GetVerifyIdentityResult(
           CancellationToken ct,
           [HttpTrigger(AuthorizationLevel.Function, "get", Route = "identityverification/{resultId}")] HttpRequestMessage request,
           string resultId,
           ExecutionContext ctx,
           [Dependency(typeof(IGetEventStoreConnectionService))] IGetEventStoreConnectionService getEventStoreConnectionService,
           ILogger logger)
        {
            var eventStoreConnection = await getEventStoreConnectionService.GetEventStoreConnection(logger);
            var eventReadResult = await eventStoreConnection.ReadEventAsync($"message-{resultId}", 0, true, new UserCredentials(EventStoreSettings.Username, EventStoreSettings.Password));
            if (!eventReadResult.Event.HasValue)
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
            var events = await eventStoreConnection
                .ReadStreamEventsForwardAsync($"$bc-{eventReadResult.Event.Value.Event.Metadata.ParseJson<IDictionary<string, object>>()[EventHeaderKey.CorrelationId]}", 0, 4096, true,
                    new UserCredentials(EventStoreSettings.Username, EventStoreSettings.Password));
            var messageByMessageType = events.Events.ToLookup(x => x.Event.EventType);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    new Dictionary<string, object>
                    {
                        {
                            "cmdCorrelationId", messageByMessageType["verifyidentity"].Last().Event.Metadata.ParseJson<IDictionary<string, object>>()[EventHeaderKey.CorrelationId]
                        },
                        {
                            "evtCorrelationId", messageByMessageType["verifyidentitysucceeded"].Last().Event.Metadata.ParseJson<IDictionary<string, object>>()[EventHeaderKey.CorrelationId]
                        }
                    }.ToJson(),
                    Encoding.UTF8, "application/json"
                )
            };
            return response;
        }

        [FunctionName(nameof(GetQueue))]
        public static async Task<HttpResponseMessage> GetQueue(
           CancellationToken ct,
           [HttpTrigger(AuthorizationLevel.Function, "get", Route = "queue/{queueId}")] HttpRequestMessage request,
           string queueId,
           ExecutionContext ctx,
           [Dependency(typeof(IGetEventStoreConnectionService))] IGetEventStoreConnectionService getEventStoreConnectionService,
           ILogger logger)
        {
            var eventStoreConnection = await getEventStoreConnectionService.GetEventStoreConnection(logger);
            var eventReadResult = await eventStoreConnection.ReadEventAsync($"message-{queueId}", 0, true,
                new UserCredentials(EventStoreSettings.Username, EventStoreSettings.Password));
            if (!eventReadResult.Event.HasValue || !Equals(eventReadResult.Event.Value.Event.EventType, "operationtimedout"))
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
            var events = await eventStoreConnection.ReadStreamEventsForwardAsync($"$bc-{eventReadResult.Event.Value.Event.Metadata.ParseJson<IDictionary<string, object>>()[EventHeaderKey.CorrelationId]}", 0, 4096, true,
                new UserCredentials(EventStoreSettings.Username, EventStoreSettings.Password));
            var messageByMessageType = events.Events.ToLookup(x => x.Event.EventType);         
            var operationTimeoutData = eventReadResult.Event.Value.Event.Data.ParseJson<IDictionary<string, object>>();
            var operationCompletionMessageTypes = ((JArray) operationTimeoutData["operationCompletionMessageTypes"]).ToObject<string[]>();
            var completionMessageType = messageByMessageType.Select(x => x.Key).FirstOrDefault(operationCompletionMessageTypes.Contains);
            if (Equals(completionMessageType, default(string)))
            {
                var response1 = new HttpResponseMessage(HttpStatusCode.OK);
                return response1;
            }
            var response2 = new HttpResponseMessage(HttpStatusCode.OK);
            var completionMessage = messageByMessageType[completionMessageType].Last();
            response2.Headers.Location = new Uri((string)operationTimeoutData["baseResultUri"] + "/" + completionMessage.Event.EventId);
            return response2;
        }

        [FunctionName(nameof(Webhook))]
        public static  HttpResponseMessage Webhook(
           CancellationToken ct,
           [HttpTrigger(AuthorizationLevel.Function, "post", Route = "webhook")] HttpRequestMessage request,
           ExecutionContext ctx,
           ILogger logger)
        {
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

}



