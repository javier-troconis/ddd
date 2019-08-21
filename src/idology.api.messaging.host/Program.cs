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
                    var eventStoreConnectionUri = new Uri($"tcp://{EventStoreSettings.Username}:{EventStoreSettings.Password}@{EventStoreSettings.ClusterDns}:2112");
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
                                    var correlationId =
                                        (string) x.Event.Metadata.ParseJson<IDictionary<string, object>>()[
                                            EventHeaderKey.CorrelationId];

                                    // 
                                    //var client = new HttpClient();
                                    //var response = await client.PostAsync("http://localhost:7072/api/v1/identityverification",
                                    //    new StringContent(Encoding.UTF8.GetString(x.Event.Data), Encoding.UTF8, "application/json"));
                                    //var responseContent = await response.Content.ReadAsByteArrayAsync();
                                    //

                                    await connection.AppendToStreamAsync($"message-{Guid.NewGuid()}", ExpectedVersion.NoStream,
                                        new[]
                                        {
                                            new EventData(Guid.NewGuid(), "verifyidentitysucceeded", false, x.Event.Data, 
                                                x.Event.Metadata.ParseJson<IDictionary<string, object>>()
                                                    .Merge(new Dictionary<string, object>
                                                    {
                                                        [EventHeaderKey.CausationId] = x.Event.EventId
                                                    }).ToJsonBytes())
                                        },
                                        new UserCredentials(EventStoreSettings.Username, EventStoreSettings.Password)
                                    );
                                    Console.WriteLine("verifyidentitysucceeded: " + correlationId);
                                })
                            .RegisterPersistentSubscriber("callbackclient", "$ce-message", "callbackclient",
                                async x =>
                                {
                                    var correlationId =
                                        x.Event.Metadata.ParseJson<IDictionary<string, object>>()[
                                            EventHeaderKey.CorrelationId];
                                    var messages = new List<ResolvedEvent>();
                                    do
                                    {
                                        var streamEventsSlice = await connection
                                            .ReadStreamEventsForwardAsync(
                                                $"$bc-{correlationId}",
                                                messages.Count, 4096, true,
                                                new UserCredentials(EventStoreSettings.Username,
                                                    EventStoreSettings.Password));
                                        messages.AddRange(streamEventsSlice.Events);
                                    } while (!messages.Select(x1 => x1.Event.EventId).Contains(x.Event.EventId));
                                    var messageByMessageType = messages.ToLookup(x1 => x1.Event.EventType);
                                    if (!messageByMessageType.Contains("clientcallbackrequested") || messageByMessageType.Contains("clientcallbackstarted"))
                                    {
                                        return;
                                    }
                                    var clientRequestTimedoutMessage = messageByMessageType["clientrequesttimedout"].Last();
                                    var clientRequestTimedoutData = clientRequestTimedoutMessage.Event.Data.ParseJson<IDictionary<string, object>>();
                                    var operationCompletionMessageTypes = ((JArray)clientRequestTimedoutData["operationCompletionMessageTypes"]).ToObject<string[]>();
                                    var completionMessageType = messageByMessageType.Select(x1 => x1.Key).FirstOrDefault(operationCompletionMessageTypes.Contains);
                                    if (Equals(completionMessageType, default(string)))
                                    {
                                        return;
                                    }
                                    var callbackClientRequestedMessage = messageByMessageType["clientcallbackrequested"].Last();
                                    var callbackClientRequestedData = callbackClientRequestedMessage.Event.Data.ParseJson<IDictionary<string, object>>();
                                    var scriptId = callbackClientRequestedData["scriptId"];
                                    var scriptStreamName = $"message-{scriptId}";
                                    var completionMessage = messageByMessageType[completionMessageType].Last();
                                    try
                                    {
                                        await connection.AppendToStreamAsync(scriptStreamName, ExpectedVersion.NoStream,
                                            new[]
                                            {
                                                new EventData(Guid.NewGuid(), "clientcallbackstarted", false, new byte[0], x.Event.Metadata)
                                            },
                                            new UserCredentials(EventStoreSettings.Username, EventStoreSettings.Password)
                                        );
                                        Console.WriteLine("clientcallbackstarted: " + correlationId);

                                        var client = new HttpClient();
                                        await client.PostAsync(
                                            (string)callbackClientRequestedData["clientUri"],
                                            new StringContent((string)clientRequestTimedoutData["baseResultUri"] + "/" + (StreamId)completionMessage.Event.EventStreamId));

                                        await connection.AppendToStreamAsync(scriptStreamName, ExpectedVersion.StreamExists,
                                            new[]
                                            {
                                                new EventData(Guid.NewGuid(), "clientcallbacksucceeded", false, new byte[0], x.Event.Metadata)
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
