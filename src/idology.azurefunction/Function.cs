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
	    [FunctionName(nameof(EvaluateOfacCompliance))]
	    public static async Task<HttpResponseMessage> EvaluateOfacCompliance(
	        CancellationToken ct, 
	        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "x")] HttpRequestMessage request, 
            ExecutionContext ctx,
            [Dependency(typeof(IEventStoreConnectionProvider))] IEventStoreConnectionProvider eventStoreConnectionProvider,
            [Dependency(typeof(IEventSourceBlockFactory))] IEventSourceBlockFactory eventSourceBlockFactory, 
	        ILogger logger)
	    {

	        var body = await request.Content.ReadAsStringAsync();
	        var data = body.ParseJson<Dictionary<string, string>>();
	        var timeout = int.Parse(data["timeout"]);
	        var callbackUri = data["callbackUri"];



            var ct1 = new CancellationTokenSource(timeout);
            var correlationId = ctx.InvocationId.ToString();
            var eventSourceBlock = await eventSourceBlockFactory.CreateEventSourceBlock(logger,
                x => Equals(x.Event.EventType, "verifyidentitysucceeded") && 
                        Equals(x.Event.Metadata.ParseJson<IDictionary<string, object>>()[EventHeaderKey.CorrelationId], correlationId));

	        
            var eventStoreConnection = await eventStoreConnectionProvider.ProvideEventStoreConnection(logger);
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
                var @event = await eventSourceBlock.ReceiveAsync(ct1.Token);
	            var response1 = new HttpResponseMessage(HttpStatusCode.OK);
                response1.Headers.Location = new Uri($"http://localhost:7071/x/{@event.Event.EventId}");
	            response1.Content = new StringContent(
	                new Dictionary<string, object>
	                {
	                    {
                            "commandCorrelationId", correlationId
                        },
	                    {
                            "eventCorrelationId",
                                    @event.Event.Metadata.ParseJson<IDictionary<string, object>>()[
                                        EventHeaderKey.CorrelationId]
                        }
	                }.ToJson(),
                    Encoding.UTF8, "application/json"
	            );
                return response1;
            }
	        catch (TaskCanceledException)
	        {
	            var eventId1 = Guid.NewGuid();
	            await eventStoreConnection.AppendToStreamAsync($"message-{eventId1}", ExpectedVersion.NoStream,
	                new[]
	                {
	                    new EventData(eventId1, "tasktimedout", false,
	                        new Dictionary<string, object>
	                        {
	                            {
                                    "taskName", "verifyidentity"
	                            },
                                {
	                                "taskCompletionMessageTypes", new []{ "verifyidentitysucceeded" }
	                            },
	                            {
	                                /*"callbackUri", "http://localhost:7071/callback"*/
                                    "callbackUri", callbackUri
                                },
	                            {
	                                "resultUri", "http://localhost:7071/x"
	                            }
                            }.ToJsonBytes(),
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
                var response2 = new HttpResponseMessage(HttpStatusCode.Accepted);
                response2.Headers.Location = new Uri($"http://localhost:7071/queue/{eventId1}");
	            return response2;
	        }
	    }

        [FunctionName(nameof(GetOfacCompliance))]
        public static async Task<HttpResponseMessage> GetOfacCompliance(
           CancellationToken ct,
           [HttpTrigger(AuthorizationLevel.Function, "get", Route = "x/{xId}")] HttpRequestMessage request,
           string xId,
           ExecutionContext ctx,
           [Dependency(typeof(IEventStoreConnectionProvider))] IEventStoreConnectionProvider eventStoreConnectionProvider,
           ILogger logger)
        {
            var eventStoreConnection = await eventStoreConnectionProvider.ProvideEventStoreConnection(logger);
            var eventReadResult = await eventStoreConnection.ReadEventAsync($"message-{xId}", 0, true,
                new UserCredentials(EventStoreSettings.Username, EventStoreSettings.Password));
            if (!eventReadResult.Event.HasValue)
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
            var events = await eventStoreConnection.ReadStreamEventsForwardAsync($"$bc-{eventReadResult.Event.Value.Event.Metadata.ParseJson<IDictionary<string, object>>()[EventHeaderKey.CorrelationId]}", 0, 4096, true,
                new UserCredentials(EventStoreSettings.Username, EventStoreSettings.Password));
            var messageByMessageType = events.Events.ToDictionary(x => x.Event.EventType, x => x.Event);

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    new Dictionary<string, object>
                    {
                        {
                            "commandCorrelationId",
                            messageByMessageType["verifyidentity"].Metadata.ParseJson<IDictionary<string, object>>()[
                                EventHeaderKey.CorrelationId]
                        },
                        {
                            "eventCorrelationId",
                            messageByMessageType["verifyidentitysucceeded"].Metadata
                                .ParseJson<IDictionary<string, object>>()[
                                    EventHeaderKey.CorrelationId]
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
           [Dependency(typeof(IEventStoreConnectionProvider))] IEventStoreConnectionProvider eventStoreConnectionProvider,
           ILogger logger)
        {
            var eventStoreConnection = await eventStoreConnectionProvider.ProvideEventStoreConnection(logger);
            var eventReadResult = await eventStoreConnection.ReadEventAsync($"message-{queueId}", 0, true,
                new UserCredentials(EventStoreSettings.Username, EventStoreSettings.Password));
            if (!eventReadResult.Event.HasValue || !Equals(eventReadResult.Event.Value.Event.EventType, "tasktimedout"))
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
            var events = await eventStoreConnection.ReadStreamEventsForwardAsync($"$bc-{eventReadResult.Event.Value.Event.Metadata.ParseJson<IDictionary<string, object>>()[EventHeaderKey.CorrelationId]}", 0, 4096, true,
                new UserCredentials(EventStoreSettings.Username, EventStoreSettings.Password));
            var messageByMessageType = events.Events.ToDictionary(x => x.Event.EventType, x => x.Event);         
            var data = eventReadResult.Event.Value.Event.Data.ParseJson<IDictionary<string, object>>();
            var taskCompletionMessageTypes = ((JArray) data["taskCompletionMessageTypes"]).ToObject<string[]>();
            var completionMessageType = messageByMessageType.Select(x => x.Key).FirstOrDefault(taskCompletionMessageTypes.Contains);
            if (Equals(completionMessageType, default(string)))
            {
                var response1 = new HttpResponseMessage(HttpStatusCode.OK);
                return response1;
            }
            var response2 = new HttpResponseMessage(HttpStatusCode.OK);
            response2.Headers.Location = new Uri((string)data["resultUri"] + "/" + messageByMessageType[completionMessageType].EventId);
            return response2;
        }

        [FunctionName(nameof(Callback))]
        public static  HttpResponseMessage Callback(
           CancellationToken ct,
           [HttpTrigger(AuthorizationLevel.Function, "post", Route = "callback")] HttpRequestMessage request,
           ExecutionContext ctx,
           ILogger logger)
        {
            //var content = await request.Content.ReadAsStringAsync();
            //logger.LogTrace($"***** {nameof(Callback)}: {content} *****" );
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

}



