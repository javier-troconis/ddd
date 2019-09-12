using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using eventstore;
using System.Linq;
using System.Net;
using System.Threading.Tasks.Dataflow;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using shared;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace idology.azurefunction
{
    public class EventStoreSendCommandService : ISendCommandService
    {
        private readonly Func<EventStoreObjectName, ILogger, CreateEventReceiver> _createEventReceiverFactory;
        private readonly Func<ILogger, Task<IEventStoreConnection>> _createEventStoreConnection;

        public EventStoreSendCommandService(Func<EventStoreObjectName, ILogger, CreateEventReceiver> createEventReceiverFactory, Func<ILogger, Task<IEventStoreConnection>> createEventStoreConnection)
        {
            _createEventReceiverFactory = createEventReceiverFactory;
            _createEventStoreConnection = createEventStoreConnection;
        }

        public async Task<HttpResponseMessage> SendCommand(Guid correlationId, Command command, string[] commandCompletionMessageTypes, ILogger logger, CancellationTokenSource cts, Uri resultBaseUri, Uri queueBaseUri, Uri callbackUri = null)
        {
            var createEventReceiver = _createEventReceiverFactory("$ce-message", logger);
            var createEventReceiverTask = createEventReceiver(
                x => x.Event.TryGetCorrelationId(out var eventCorrelationId) 
                     && Equals(eventCorrelationId, correlationId.ToString()) 
                     && commandCompletionMessageTypes.Contains(x.Event.EventType));
            var eventStoreConnectionTask = _createEventStoreConnection(logger);
            await Task.WhenAll(createEventReceiverTask, eventStoreConnectionTask);
            var eventReceiver = await createEventReceiverTask;
            var eventStoreConnection = await eventStoreConnectionTask;
            await eventStoreConnection.AppendToStreamAsync($"message-{Guid.NewGuid()}", ExpectedVersion.NoStream,
                new UserCredentials(EventStoreSettings.Username, EventStoreSettings.Password),
                new EventData(command.CommandId, command.CommandName, false, command.CommandData,
                    new Dictionary<string, object>
                    {
                        [EventHeaderKey.CorrelationId] = correlationId,
                        [EventHeaderKey.CausationId] = command.CommandId
                    }.ToJsonBytes()
                )
            );
            try
            {
                var resolvedEvent = await eventReceiver.ReceiveAsync(cts.Token);
                var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
                httpResponseMessage.Headers.Location = new Uri(resultBaseUri + "/" + (StreamId)resolvedEvent.Event.EventStreamId);
                httpResponseMessage.Headers.Add("command-correlation-id", correlationId.ToString());
                httpResponseMessage.Headers.Add("event-correlation-id", resolvedEvent.Event.TryGetCorrelationId(out var eventCorrelationId) ? eventCorrelationId : string.Empty);
                httpResponseMessage.Content = new StringContent(Encoding.UTF8.GetString(resolvedEvent.Event.Data), Encoding.UTF8, "application/json");
                return httpResponseMessage;
            }
            catch (TaskCanceledException)
            {
                var queueId = Guid.NewGuid();
                var clientRequestTimedoutTask = eventStoreConnection.AppendToStreamAsync($"message-{queueId}", ExpectedVersion.NoStream,
                    new UserCredentials(EventStoreSettings.Username, EventStoreSettings.Password),
                        new EventData(Guid.NewGuid(), "clientrequesttimedout", false,
                            new Dictionary<string, object>
                            {
                                ["operationName"] = command.CommandName,
                                ["operationCompletionMessageTypes"] = commandCompletionMessageTypes,
                                ["resultBaseUri"] = resultBaseUri
                            }.ToJsonBytes(),
                            new Dictionary<string, object>
                            {
                                [EventHeaderKey.CorrelationId] = correlationId,
                                [EventHeaderKey.CausationId] = command.CommandId
                            }.ToJsonBytes()
                        )
                );
                var clientCallbackRequestedTask = callbackUri == null ? Task.CompletedTask :
                    eventStoreConnection.AppendToStreamAsync($"message-{Guid.NewGuid()}", ExpectedVersion.NoStream,
                        new UserCredentials(EventStoreSettings.Username, EventStoreSettings.Password),
                        new EventData(Guid.NewGuid(), "clientcallbackrequested", false,
                            new Dictionary<string, object>
                            {
                                ["operationName"] = command.CommandName,
                                ["operationCompletionMessageTypes"] = commandCompletionMessageTypes,
                                ["resultBaseUri"] = resultBaseUri,
                                ["clientUri"] = callbackUri,
                                ["scriptId"] = Guid.NewGuid()
                            }.ToJsonBytes(),
                            new Dictionary<string, object>
                            {
                                [EventHeaderKey.CorrelationId] = correlationId,
                                [EventHeaderKey.CausationId] = command.CommandId
                            }.ToJsonBytes()
                        )
                    );
                await Task.WhenAll(clientRequestTimedoutTask, clientCallbackRequestedTask);
                var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.Accepted);
                httpResponseMessage.Headers.Location = new Uri($"{queueBaseUri.AbsoluteUri}/{queueId}");
                return httpResponseMessage;
            }
        }

     
    }
}

/*
	        var eventReceivers = await Task.WhenAll(commandCompletionMessageTypes.Select(x =>
	            {
	                var createEventReceiver = createEventReceiverFactory($"$et-{x}", logger);
	                var eventReceiver = createEventReceiver(y =>
	                    Equals(y.Event.Metadata.ParseJson<IDictionary<string, object>>()[EventHeaderKey.CorrelationId],
	                        correlationId));
                    return eventReceiver;
	            }
	        ));
	        Func<CancellationTokenSource, Task<ResolvedEvent>> receiveEvent = async cts1 =>
	        {
	            var receivedEventTask = await Task.WhenAny(eventReceivers.Select(x1 => x1.ReceiveAsync(cts1.Token)));
                cts1.Cancel();
	            return  await receivedEventTask;
	        };
            */
