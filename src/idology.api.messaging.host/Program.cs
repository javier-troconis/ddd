using System;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
                var rnd = new Random();
                var connection = CreateConnection();   
                var eventBus = EventBus.CreateEventBus
                (
                    CreateConnection,
                    registry => 
                        registry
                            .RegisterPersistentSubscriber("verifyidentity", "$et-verifyidentity", "verifyidentity",
                                async x =>
                                {
                                    //await Task.Delay(rnd.Next(500, 1000));
                                    var eventId = Guid.NewGuid();
                                    await connection.AppendToStreamAsync($"message-{eventId}", ExpectedVersion.NoStream,
                                        new[]
                                        {
                                            new EventData(eventId, "verifyidentitysucceeded", false, new byte[0], x.Event.Metadata)
                                        },
                                        new UserCredentials(EventStoreSettings.Username, EventStoreSettings.Password)
                                    );
                                    Console.WriteLine("verifyidentity completed: " + (string)x.Event.Metadata.ParseJson<IDictionary<string, object>>()[EventHeaderKey.CorrelationId]);
                                })
                            .RegisterPersistentSubscriber("pushresulttoclient", "$et-pushresulttoclient", "pushresulttoclient",
                                async x =>
                                {
                                    //await Task.Delay(rnd.Next(500, 1000));
                                    var data = x.Event.Data.ParseJson<IDictionary<string, object>>();
                                    var client = new HttpClient();
                                        var request = new HttpRequestMessage(HttpMethod.Post, (string)data["clientUri"])
                                        {
                                            Content = new StringContent((string)data["resultUri"])
                                        };
                                    await client.SendAsync(request);
                                    var eventId = Guid.NewGuid();
                                    await connection.AppendToStreamAsync($"message-{eventId}",
                                            ExpectedVersion.NoStream,
                                            new[]
                                            {
                                                new EventData(eventId, "pushresulttoclientsucceeded", false, new byte[0], x.Event.Metadata)
                                            },
                                            new UserCredentials(EventStoreSettings.Username, EventStoreSettings.Password)
                                        );
                                    Console.WriteLine("pushresulttoclient completed: " + (string)x.Event.Metadata.ParseJson<IDictionary<string, object>>()[EventHeaderKey.CorrelationId]);
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
                                    if (!messageByMessageType.Contains("callbackclientrequested") || messageByMessageType.Contains("callbackclientstarted"))
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
                                    var callbackClientRequestedMessage = messageByMessageType["callbackclientrequested"].Last();
                                    var callbackClientRequestedData = callbackClientRequestedMessage.Event.Data.ParseJson<IDictionary<string, object>>();
                                    var scriptId = callbackClientRequestedData["scriptId"];
                                    var scriptStreamName = $"message-{scriptId}";
                                    var completionMessage = messageByMessageType[completionMessageType].Last();
                                    try
                                    {
                                        using (var tx = await connection.StartTransactionAsync(
                                            scriptStreamName, ExpectedVersion.NoStream,
                                            new UserCredentials(EventStoreSettings.Username,
                                                EventStoreSettings.Password)))
                                        {
                                            await tx.WriteAsync
                                            (
                                                new EventData(Guid.NewGuid(), "callbackclientstarted",
                                                    false,
                                                    new byte[0],
                                                    x.Event.Metadata
                                                )
                                            );

                                            var client = new HttpClient();
                                            await client.PostAsync((string)callbackClientRequestedData["clientUri"], new StringContent($"{(string)clientRequestTimedoutData["baseResultUri"]}/{completionMessage.Event.EventId}"));

                                            await tx.WriteAsync
                                            (
                                                new EventData(Guid.NewGuid(), "callbackclientsucceeded",
                                                    false,
                                                    new byte[0],
                                                    x.Event.Metadata
                                                )
                                            );
                                        }

                                        /*
                                        await connection.AppendToStreamAsync(
                                            scriptStreamName, 
                                            scriptExpectedVersion,
                                            new[]
                                            {
                                                new EventData(Guid.NewGuid(), "pushresulttoclient", false, 
                                                    new Dictionary<string, object>
                                                    {
                                                        { "clientUri", callbackClientRequestedData["clientUri"] },
                                                        { "resultUri", $"{(string)clientRequestTimedoutData["baseResultUri"]}/{completionMessage.Event.EventId}" }
                                                    }.ToJsonBytes(), 
                                                    x.Event.Metadata)
                                            },
                                            new UserCredentials(EventStoreSettings.Username, EventStoreSettings.Password)
                                        );
                                        */
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
