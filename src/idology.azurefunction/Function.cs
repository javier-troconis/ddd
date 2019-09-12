﻿using azurefunction;
using eventstore;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using shared;
using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace idology.azurefunction
{
    public static class Function
	{
	    [FunctionName(nameof(VerifyIdentity))]
	    public static async Task<HttpResponseMessage> VerifyIdentity(
	        CancellationToken ct, 
	        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "identityverification")] HttpRequestMessage request, 
            ExecutionContext ctx,
	        [Dependency(typeof(ISendCommandService))] ISendCommandService sendCommandService,
            ILogger logger)
	    {
	        var requestTimeout = int.Parse(request.Headers.FirstOrDefault(x => x.Key == "request-timeout").Value.First());
	        var callbackUri = request.Headers.Contains("callback-uri") ? new Uri(request.Headers.First(x => x.Key == "callback-uri").Value.First()) : null;
	        var requestContent = await request.Content.ReadAsByteArrayAsync();
	        var correlationId = Guid.NewGuid();
	        var command = new Command(Guid.NewGuid(), "verifyidentity", requestContent);
	        var commandCompletionMessageTypes = new[] {"verifyidentitysucceeded", "verifyidentityfailed"};
	        var eventReceiveCts = new CancellationTokenSource(requestTimeout);
	        var resultBaseUri = new Uri("http://localhost:7071/identityverification");
	        var queueBaseUri = new Uri("http://localhost:7071/queue");
            return await sendCommandService.SendCommand(correlationId, command, commandCompletionMessageTypes, logger, eventReceiveCts, resultBaseUri, queueBaseUri, callbackUri);

	        /*
             
            
            
            var correlationId = ctx.InvocationId.ToString();
            var cts = new CancellationTokenSource(requestTimeout);
            var commandId = Guid.NewGuid();
            var commandName = "verifyidentity";
            var commandCompletionMessageTypes = new[] { "verifyidentitysucceeded" };
            var resultBaseUri = "http://localhost:7071/identityverification";
            var connection = await createEventStoreConnection(logger);
            var content = await request.Content.ReadAsByteArrayAsync();

            
            
            var createEventReceiver = createEventReceiverFactory("$ce-message", logger);
            var eventReceiver = await createEventReceiver(
                x => Equals(x.Event.Metadata.ParseJson<IDictionary<string, object>>()[EventHeaderKey.CorrelationId], correlationId) &&
                     commandCompletionMessageTypes.Contains(x.Event.EventType));
            Func<CancellationTokenSource, Task<ResolvedEvent>> receiveEvent = cts1 => eventReceiver.ReceiveAsync(cts1.Token);
            
            
           
            try
            {
                await connection.AppendToStreamAsync($"message-{Guid.NewGuid()}", ExpectedVersion.NoStream,
                    new UserCredentials(EventStoreSettings.Username, EventStoreSettings.Password),
                        new EventData(commandId, commandName, false, content,
                            new Dictionary<string, object>
                            {
                                [EventHeaderKey.CorrelationId] = correlationId,
                                [EventHeaderKey.CausationId] = commandId
                            }.ToJsonBytes()
                        )
                    );
                var resolvedEvent = await receiveEvent(cts);
                var response1 = new HttpResponseMessage(HttpStatusCode.OK);
                response1.Headers.Location = new Uri(resultBaseUri + "/" +(StreamId)resolvedEvent.Event.EventStreamId);
                response1.Headers.Add("command-correlation-id", correlationId);
                response1.Headers.Add("event-correlation-id", (string)resolvedEvent.Event.Metadata.ParseJson<IDictionary<string, object>>()[EventHeaderKey.CorrelationId]);
                response1.Content = new StringContent(Encoding.UTF8.GetString(resolvedEvent.Event.Data), Encoding.UTF8, "application/json");
                return response1;
            }
            catch (TaskCanceledException)
            {
                var queueId = Guid.NewGuid();
                var clientRequestTimedoutTask = connection.AppendToStreamAsync($"message-{queueId}", ExpectedVersion.NoStream,
                    new UserCredentials(EventStoreSettings.Username, EventStoreSettings.Password),
                        new EventData(Guid.NewGuid(), "clientrequesttimedout", false,
                            new Dictionary<string, object>
                            {
                                ["operationName"] = commandName,
                                ["operationCompletionMessageTypes"] = commandCompletionMessageTypes,
                                ["resultBaseUri"] = resultBaseUri
                            }.ToJsonBytes(),
                            new Dictionary<string, object>
                            {
                                [EventHeaderKey.CorrelationId] = correlationId,
                                [EventHeaderKey.CausationId] = commandId
                            }.ToJsonBytes()
                        )
                );
                var clientCallbackRequestedTask = callbackUri == null ? Task.CompletedTask : 
                    connection.AppendToStreamAsync($"message-{Guid.NewGuid()}", ExpectedVersion.NoStream,
                        new UserCredentials(EventStoreSettings.Username, EventStoreSettings.Password),
                        new EventData(Guid.NewGuid(), "clientcallbackrequested", false,
                            new Dictionary<string, object>
                            {
                                ["operationName"] = commandName,
                                ["operationCompletionMessageTypes"] = commandCompletionMessageTypes,
                                ["resultBaseUri"] = resultBaseUri,
                                ["clientUri"] = callbackUri,
                                ["scriptId"] = Guid.NewGuid()
                            }.ToJsonBytes(),
                            new Dictionary<string, object>
                            {
                                [EventHeaderKey.CorrelationId] = correlationId,
                                [EventHeaderKey.CausationId] = commandId
                            }.ToJsonBytes()
                        )
                    );
                await Task.WhenAll(clientRequestTimedoutTask, clientCallbackRequestedTask);
                var response2 = new HttpResponseMessage(HttpStatusCode.Accepted);
                response2.Headers.Location = new Uri($"http://localhost:7071/queue/{queueId}");
                return response2;
            }
            */
	    }

        [FunctionName(nameof(GetResult))]
        public static async Task<HttpResponseMessage> GetResult(
           CancellationToken ct,
           [HttpTrigger(AuthorizationLevel.Function, "get", Route = "{resultType}/{resultId}")] HttpRequestMessage request,
           string resultType,
           string resultId,
           ExecutionContext ctx,
           [Dependency(typeof(IReadMessagesService<ResolvedEvent>))] IReadMessagesService<ResolvedEvent> createEventStoreConnection,
           ILogger logger)
        {
            var getResultByResultType = new Dictionary<string, Func<ResolvedEvent[], byte[]>>
            {
                ["identityverification"] = x => x[x.Length - 1].Event.Data
            };

            var connection = await createEventStoreConnection(logger);
            var eventReadResult = await connection.ReadEventAsync($"message-{resultId}", 0, true, new UserCredentials(EventStoreSettings.Username, EventStoreSettings.Password));
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
            var causationResolvedEvent = eventsSlice.Events[0];
            var response1 = new HttpResponseMessage(HttpStatusCode.OK);
            var responseContent = getResultByResultType[resultType](eventsSlice.Events);
            response1.Headers.Location = request.RequestUri;
            response1.Headers.Add("command-correlation-id", (string)causationResolvedEvent.Event.Metadata.ParseJson<IDictionary<string, object>>()[
                EventHeaderKey.CorrelationId]);
            response1.Headers.Add("event-correlation-id", (string)correlationId);
            response1.Content = new StringContent(Encoding.UTF8.GetString(responseContent), Encoding.UTF8, "application/json");
            return response1;
        }

        [FunctionName(nameof(GetQueue))]
        public static async Task<HttpResponseMessage> GetQueue(
           CancellationToken ct,
           [HttpTrigger(AuthorizationLevel.Function, "get", Route = "queue/{queueId}")] HttpRequestMessage request,
           string queueId,
           ExecutionContext ctx,
           [Dependency(typeof(CreateEventStoreConnection))] CreateEventStoreConnection createEventStoreConnection,
           ILogger logger)
        {
            var connection = await createEventStoreConnection(logger);
            var eventReadResult = await connection.ReadEventAsync($"message-{queueId}", 0, true,
                new UserCredentials(EventStoreSettings.Username, EventStoreSettings.Password));
            if (eventReadResult.Event == null || !Equals(eventReadResult.Event.Value.Event.EventType, "clientrequesttimedout"))
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
            var clientRequestTimedOutMessage = (ResolvedEvent)eventReadResult.Event;
            var correlationId =
                clientRequestTimedOutMessage.Event.Metadata.ParseJson<IDictionary<string, object>>()[
                    EventHeaderKey.CorrelationId];
            var events = await connection.ReadStreamEventsForwardAsync($"$bc-{correlationId}", 0, 4096, true,
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
            response1.Headers.Location = new Uri((string)clientRequestTimedOutData["resultBaseUri"] + "/" + (StreamId)completionMessage.Event.EventStreamId);
            return response1;
        }

        [FunctionName(nameof(Webhook))]
        public static async Task<HttpResponseMessage> Webhook(
           CancellationToken ct,
           [HttpTrigger(AuthorizationLevel.Function, "post", Route = "webhook")] HttpRequestMessage request,
           ExecutionContext ctx,
           ILogger logger)
        {
            var requestContent = await request.Content.ReadAsStringAsync();
            logger.LogInformation($"{nameof(Webhook)} received: {requestContent}");
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

}



