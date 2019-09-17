using azurefunction;
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
	        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "identityverification/{providername}")] HttpRequestMessage request, 
            string providerName,
	        ExecutionContext ctx,
	        [Dependency(typeof(ISendCommandService))] ISendCommandService sendCommandService,
            ILogger logger)
	    {
	        var requestTimeout = int.Parse(request.Headers.FirstOrDefault(x => x.Key == "request-timeout").Value.First());
	        var callbackUri = request.Headers.Contains("callback-uri") ? new Uri(request.Headers.First(x => x.Key == "callback-uri").Value.First()) : null;
	        var requestContent = await request.Content.ReadAsByteArrayAsync();
	        var correlationId = ctx.InvocationId;
	        var commandId = Guid.NewGuid();
	        var command = new Command("verifyidentity", requestContent);
	        var commandCompletionMessageTypes = new[] {"verifyidentitysucceeded", "verifyidentityfailed"};
	        var eventReceiveCts = new CancellationTokenSource(requestTimeout);
	        var hostBaseUri = $"{request.RequestUri.Scheme}://{request.RequestUri.Authority}";
            var resultBaseUri = new Uri($"{hostBaseUri}/identityverification");
	        var queueBaseUri = new Uri($"{hostBaseUri}/queue");
            return await sendCommandService
                .SendCommand(correlationId, commandId, command, commandCompletionMessageTypes, logger, eventReceiveCts, resultBaseUri, queueBaseUri, callbackUri, 
                    new Dictionary<string, object>
                    {
                        ["provider-name"] = providerName
                    });
	    }

	    private static readonly Dictionary<string, Func<ResolvedEvent[], byte[]>> AggregatorByMessageTypeMap =
	        new Dictionary<string, Func<ResolvedEvent[], byte[]>>
	        {
	            ["identityverification"] = x => x[x.Length - 1].Event.Data
	        };

        [FunctionName(nameof(GetResult))]
        public static async Task<HttpResponseMessage> GetResult(
           CancellationToken ct,
           [HttpTrigger(AuthorizationLevel.Function, "get", Route = "{resultType}/{resultId}")] HttpRequestMessage request,
           string resultType,
           string resultId,
           ExecutionContext ctx,
           [Dependency(typeof(IReadMessagesService<ResolvedEvent>))] IReadMessagesService<ResolvedEvent> readMessagesService,
           ILogger logger)
        {
            var messages = await readMessagesService.ReadMessages($"message-{resultId}", logger);
            if (messages == null || !messages.Any())
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
            var resultEvent = messages[0];
            resultEvent.Event.TryGetCorrelationId(out var correlationId);
            var correlationEvents = await readMessagesService.ReadMessages($"$bc-{correlationId}", logger);
            var causationResolvedEvent = correlationEvents[0];
            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            var responseContent = AggregatorByMessageTypeMap[resultType](correlationEvents);
            httpResponseMessage.Headers.Location = request.RequestUri;
            httpResponseMessage.Headers.Add("command-correlation-id", causationResolvedEvent.Event.TryGetCorrelationId(out var causationCorrelationId) ? causationCorrelationId : string.Empty);
            httpResponseMessage.Headers.Add("event-correlation-id", correlationId);
            httpResponseMessage.Content = new StringContent(Encoding.UTF8.GetString(responseContent), Encoding.UTF8, "application/json");
            return httpResponseMessage;
        }

        [FunctionName(nameof(GetQueue))]
        public static async Task<HttpResponseMessage> GetQueue(
           CancellationToken ct,
           [HttpTrigger(AuthorizationLevel.Function, "get", Route = "queue/{queueId}")] HttpRequestMessage request,
           string queueId,
           ExecutionContext ctx,
           [Dependency(typeof(IReadMessagesService<ResolvedEvent>))] IReadMessagesService<ResolvedEvent> readMessagesService,
           ILogger logger)
        {
            var messages = await readMessagesService.ReadMessages($"message-{queueId}", logger);
            if (messages == null || !messages.Any() || !Equals(messages[0].Event.EventType, "clientrequesttimedout"))
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
            var clientRequestTimedOutMessage = messages[0];
            clientRequestTimedOutMessage.Event.TryGetCorrelationId(out var correlationId);  
            var correlationEvents = await readMessagesService.ReadMessages($"$bc-{correlationId}", logger);
            var messageByMessageType = correlationEvents.ToLookup(x => x.Event.EventType);         
            var clientRequestTimedOutData = clientRequestTimedOutMessage.Event.Data.ParseJson<IDictionary<string, object>>();
            var operationCompletionMessageTypes = ((JArray) clientRequestTimedOutData["operationCompletionMessageTypes"]).ToObject<string[]>();
            var completionMessageType = messageByMessageType.Select(x => x.Key).FirstOrDefault(operationCompletionMessageTypes.Contains);
            if (Equals(completionMessageType, default(string)))
            {
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            var completionMessage = messageByMessageType[completionMessageType].Last();
            httpResponseMessage.Headers.Location = new Uri((string)clientRequestTimedOutData["resultBaseUri"] + "/" + (StreamId)completionMessage.Event.EventStreamId);
            return httpResponseMessage;
        }
    }

}



