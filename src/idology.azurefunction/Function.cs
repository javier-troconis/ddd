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
	        var requestTimeout = int.Parse(request.Headers.FirstOrDefault(x => x.Key == "request-timeout").Value.First());
	        var callbackUri = request.Headers.FirstOrDefault(x => x.Key == "callback-uri").Value?.First();
            var ct1 = new CancellationTokenSource(requestTimeout);
            var correlationId = ctx.InvocationId.ToString();
            var eventSourceBlock = await createEventSourceBlockService.CreateEventSourceBlock(logger,
                x => Equals(x.Event.EventType, "verifyidentitysucceeded") && 
                     Equals(x.Event.Metadata.ParseJson<IDictionary<string, object>>()[EventHeaderKey.CorrelationId], correlationId));
            var eventStoreConnection = await getEventStoreConnectionService.GetEventStoreConnection(logger);
	        var content = await request.Content.ReadAsByteArrayAsync();
            await eventStoreConnection.AppendToStreamAsync($"message-{Guid.NewGuid()}", ExpectedVersion.NoStream,
                new UserCredentials(EventStoreSettings.Username, EventStoreSettings.Password),
                    new EventData(Guid.NewGuid(), "verifyidentity", false, content,
                        new Dictionary<string, object>
                        {
                            {
                                EventHeaderKey.CorrelationId, correlationId
                            }
                        }.ToJsonBytes()
                    )
            );

	        try
	        {
                var verifyIdentityResolvedEvent = await eventSourceBlock.ReceiveAsync(ct1.Token);
	            var response1 = new HttpResponseMessage(HttpStatusCode.OK);
                response1.Headers.Location = new Uri("http://localhost:7071/identityverification/" + (StreamId)verifyIdentityResolvedEvent.Event.EventStreamId);
                response1.Headers.Add("command-correlation-id", correlationId);
	            response1.Headers.Add("event-correlation-id", (string)verifyIdentityResolvedEvent.Event.Metadata.ParseJson<IDictionary<string, object>>()[EventHeaderKey.CorrelationId]);
	            response1.Content = new StringContent(Encoding.UTF8.GetString(verifyIdentityResolvedEvent.Event.Data), Encoding.UTF8, "application/json");
                return response1;
            }
	        catch (TaskCanceledException)
	        {
	            var queueId = Guid.NewGuid();
	            await eventStoreConnection.AppendToStreamAsync($"message-{queueId}", ExpectedVersion.NoStream,
	                new UserCredentials(EventStoreSettings.Username, EventStoreSettings.Password),
	                    new EventData(Guid.NewGuid(), "clientrequesttimedout", false,
	                        new Dictionary<string, object>
	                        {
	                            {
	                                "operationName", "verifyidentity"
	                            },
	                            {
	                                "operationCompletionMessageTypes", new[] {"verifyidentitysucceeded"}
	                            },
	                            {
	                                "baseResultUri", "http://localhost:7071/identityverification"
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
	            if (callbackUri != null)
	            {
	                await eventStoreConnection.AppendToStreamAsync($"message-{Guid.NewGuid()}", ExpectedVersion.NoStream,
	                    new UserCredentials(EventStoreSettings.Username, EventStoreSettings.Password),
	                    new EventData(Guid.NewGuid(), "clientcallbackrequested", false,
	                        new Dictionary<string, object>
	                        {
	                            {
	                                "clientUri", callbackUri
	                            },
	                            {
	                                "scriptId", Guid.NewGuid()
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
                var response2 = new HttpResponseMessage(HttpStatusCode.Accepted);
                response2.Headers.Location = new Uri($"http://localhost:7071/queue/{queueId}");
	            return response2;
	        }
	    }

        [FunctionName(nameof(GetVerifyIdentityResult))]
        public static async Task<HttpResponseMessage> GetVerifyIdentityResult(
           CancellationToken ct,
           [HttpTrigger(AuthorizationLevel.Function, "get", Route = "identityverification/{transactionId}")] HttpRequestMessage request,
           string transactionId,
           ExecutionContext ctx,
           [Dependency(typeof(IGetEventStoreConnectionService))] IGetEventStoreConnectionService getEventStoreConnectionService,
           ILogger logger)
        {
            var connection = await getEventStoreConnectionService.GetEventStoreConnection(logger);
            var eventReadResult = await connection.ReadEventAsync($"message-{transactionId}", 0, true, new UserCredentials(EventStoreSettings.Username, EventStoreSettings.Password));
            if (eventReadResult.Event == null)
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
            var @event = (ResolvedEvent)eventReadResult.Event;
            var correlationId = 
                @event.Event.Metadata.ParseJson<IDictionary<string, object>>()[
                    EventHeaderKey.CorrelationId];
            var eventsSlice = await connection
                .ReadStreamEventsForwardAsync($"$bc-{correlationId}", 0, 4096, true,
                    new UserCredentials(EventStoreSettings.Username, EventStoreSettings.Password));
            var correlationOriginationResolvedEvent = eventsSlice.Events.First();
            var correlationOriginationId =
                (string) correlationOriginationResolvedEvent.Event.Metadata.ParseJson<IDictionary<string, object>>()[
                    EventHeaderKey.CorrelationId];
            var response1 = new HttpResponseMessage(HttpStatusCode.OK);
            response1.Headers.Location = new Uri($"http://localhost:7071/identityverification/{transactionId}");
            response1.Headers.Add("command-correlation-id", correlationOriginationId);
            response1.Headers.Add("event-correlation-id", (string)correlationId);
            response1.Content = new StringContent(Encoding.UTF8.GetString(eventReadResult.Event.Value.Event.Data), Encoding.UTF8, "application/json");
            return response1;
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
            if (eventReadResult.Event == null || !Equals(eventReadResult.Event.Value.Event.EventType, "clientrequesttimedout"))
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
            var clientRequestTimedOutMessage = (ResolvedEvent)eventReadResult.Event;
            var correlationId =
                clientRequestTimedOutMessage.Event.Metadata.ParseJson<IDictionary<string, object>>()[
                    EventHeaderKey.CorrelationId];
            var events = await eventStoreConnection.ReadStreamEventsForwardAsync($"$bc-{correlationId}", 0, 4096, true,
                new UserCredentials(EventStoreSettings.Username, EventStoreSettings.Password));
            var messageByMessageType = events.Events.ToLookup(x => x.Event.EventType);         
            var clientRequestTimedOutData = eventReadResult.Event.Value.Event.Data.ParseJson<IDictionary<string, object>>();
            var operationCompletionMessageTypes = ((JArray) clientRequestTimedOutData["operationCompletionMessageTypes"]).ToObject<string[]>();
            var completionMessageType = messageByMessageType.Select(x => x.Key).FirstOrDefault(operationCompletionMessageTypes.Contains);
            if (Equals(completionMessageType, default(string)))
            {
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            var response1 = new HttpResponseMessage(HttpStatusCode.OK);
            var completionMessage = messageByMessageType[completionMessageType].Last();
            response1.Headers.Location = new Uri((string)clientRequestTimedOutData["baseResultUri"] + "/" + (StreamId)completionMessage.Event.EventStreamId);
            return response1;
        }

        [FunctionName(nameof(Webhook))]
        public static HttpResponseMessage Webhook(
           CancellationToken ct,
           [HttpTrigger(AuthorizationLevel.Function, "post", Route = "webhook")] HttpRequestMessage request,
           ExecutionContext ctx,
           ILogger logger)
        {
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

}



