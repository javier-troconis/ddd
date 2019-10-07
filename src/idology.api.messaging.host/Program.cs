using System;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using eventstore;
using EventStore.ClientAPI.Exceptions;
using Newtonsoft.Json.Linq;
using shared;

namespace idology.api.messaging.host
{


    class Program
    {
     
        static void Main(string[] args)
        {
            Task.Run(async () => 
            {

                IEventStoreConnection CreateConnection()
                {
                    var eventStoreConnectionUri =
                        new Uri(
                            $"tcp://{EventStoreSettings.Username}:{EventStoreSettings.Password}@{EventStoreSettings.ClusterDns}:{EventStoreSettings.ExternalTcpPort}");
                    var connectionSettingsBuilder = ConnectionSettings.Create();
                    var connectionSettings = connectionSettingsBuilder.Build();
                    return EventStoreConnection.Create(connectionSettings, eventStoreConnectionUri);
                }

                var connection = CreateConnection();   
                var eventBus = EventBus.CreateEventBus
                (
                    CreateConnection,
                    registry => 
                        registry
                            .RegisterPersistentSubscriber("verifyidentity", "$et-verifyidentity", "verifyidentity",
                                async x =>
                                {
                                    x.Event.TryGetCorrelationId(out var correlationId);
                                    var metadata = x.Event.Metadata.ParseJson<IDictionary<string, object>>();
                                    metadata.TryGetValue("provider-name", out var providerName);
                                    dynamic service = VerifyIdentityServiceByProviderName.Value[(string)providerName];

                                    IEnumerable<Message<byte[]>> messages = await Dispatcher.Dispatch(service, x.Event.Data);
                                    var events = messages.Select(e => new EventData(Guid.NewGuid(),
                                        e.Name, false, e.Data,
                                        x.Event.Metadata.ParseJson<IDictionary<string, object>>()
                                            .Merge(new Dictionary<string, object>
                                            {
                                                [EventHeaderKey.CausationId] = x.Event.EventId
                                            }).ToJsonBytes()));

                                    await connection.AppendToStreamAsync($"message-{Guid.NewGuid()}", ExpectedVersion.NoStream, events, 
                                        new UserCredentials(EventStoreSettings.Username, EventStoreSettings.Password));
                                })
                            .RegisterPersistentSubscriber("callbackclient", "$ce-message", "callbackclient",
                                async x =>
                                {
                                    x.Event.TryGetCorrelationId(out var correlationId);

                                    var messages = new List<ResolvedEvent>();
                                    do
                                    {
                                        var eventsSlice = await connection
                                            .ReadStreamEventsForwardAsync(
                                                $"$bc-{correlationId}",
                                                messages.Count, 4096, true,
                                                new UserCredentials(EventStoreSettings.Username,
                                                    EventStoreSettings.Password));
                                        messages.AddRange(eventsSlice.Events);
                                    } while (!messages.Select(x1 => x1.Event.EventId).Contains(x.Event.EventId));
                                    var messageByMessageType = messages.ToLookup(x1 => x1.Event.EventType);
                                    if (!messageByMessageType.Contains("clientcallbackrequested") || messageByMessageType.Contains("clientcallbackstarted"))
                                    {
                                        return;
                                    }

                                    var callbackClientRequestedMessage = messageByMessageType["clientcallbackrequested"].Last();
                                    var callbackClientRequestedData = callbackClientRequestedMessage.Event.Data.ParseJson<IDictionary<string, object>>();
                                    var operationCompletionMessageTypes = ((JArray)callbackClientRequestedData["operationCompletionMessageTypes"]).ToObject<string[]>();
                                    var completionMessageType = messageByMessageType.Select(x1 => x1.Key).FirstOrDefault(operationCompletionMessageTypes.Contains);
                                    if (Equals(completionMessageType, default(string)))
                                    {
                                        return;
                                    }
                                    
                                    var scriptStreamName = $"message-{callbackClientRequestedData["scriptId"]}";
                                    var completionMessage = messageByMessageType[completionMessageType].Last();
                                    try
                                    {
                                        await connection.AppendToStreamAsync(scriptStreamName, ExpectedVersion.NoStream,
                                            new[]
                                            {
                                                new EventData(Guid.NewGuid(), "clientcallbackstarted", false, new byte[0], 
                                                    x.Event.Metadata.ParseJson<IDictionary<string, object>>()
                                                        .Merge(new Dictionary<string, object>
                                                        {
                                                            [EventHeaderKey.CausationId] = x.Event.EventId
                                                        }).ToJsonBytes())
                                            },
                                            new UserCredentials(EventStoreSettings.Username, EventStoreSettings.Password)
                                        );
                                        Console.WriteLine("clientcallbackstarted: " + correlationId);

                                        var client = new HttpClient();
                                        await client.PostAsync(
                                            (string)callbackClientRequestedData["clientUri"],
                                            new StringContent((string)callbackClientRequestedData["resultBaseUri"] + "/" + (StreamId)completionMessage.Event.EventStreamId));

                                        await connection.AppendToStreamAsync(scriptStreamName, ExpectedVersion.StreamExists,
                                            new[]
                                            {
                                                new EventData(Guid.NewGuid(), "clientcallbacksucceeded", false, new byte[0], 
                                                    x.Event.Metadata.ParseJson<IDictionary<string, object>>()
                                                        .Merge(new Dictionary<string, object>
                                                        {
                                                            [EventHeaderKey.CausationId] = x.Event.EventId
                                                        }).ToJsonBytes())
                                            },
                                            new UserCredentials(EventStoreSettings.Username, EventStoreSettings.Password)
                                        );
                                        Console.WriteLine("clientcallbacksucceeded: " + correlationId);
                                    }
                                    catch (WrongExpectedVersionException)
                                    {
                                        
                                    }
                                })
                );

                await connection.ConnectAsync();
                await eventBus.StartAllSubscribers();
            });

            while (true) { }
            
        }
    }
}
