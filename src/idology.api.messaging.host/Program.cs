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
                                x =>
                                {
                                    var eventId = Guid.NewGuid();
                                    return connection.AppendToStreamAsync($"message-{eventId}", ExpectedVersion.NoStream,
                                        new[]
                                        {
                                            new EventData(eventId, "verifyidentitysucceeded", false, new byte[0], x.Event.Metadata)
                                        },
                                        new UserCredentials(EventStoreSettings.Username, EventStoreSettings.Password)
                                    );
                                })
                            .RegisterPersistentSubscriber("pushresulttoclient", "$et-pushresulttoclient", "pushresulttoclient",
                                async x =>
                                {
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
                                })
                            .RegisterPersistentSubscriber("callbackclient", "$ce-message", "callbackclient",
                                async x =>
                                {
                                    StreamEventsSlice events;
                                    while (true)
                                    {
                                        events = await connection
                                            .ReadStreamEventsForwardAsync(
                                                $"$bc-{x.Event.Metadata.ParseJson<IDictionary<string, object>>()[EventHeaderKey.CorrelationId]}",
                                                0, 4096, true,
                                                new UserCredentials(EventStoreSettings.Username,
                                                    EventStoreSettings.Password));
                                        var eventIds = events.Events.Select(x1 => x1.Event.EventId);
                                        if (eventIds.Contains(x.Event.EventId))
                                        {
                                            break;
                                        }
                                    }
                                    var messageByMessageType = events.Events.ToLookup(x1 => x1.Event.EventType);
                                    if (!messageByMessageType.Contains("callbackclient"))
                                    {
                                        return;
                                    }
                                    var operationTimedoutMessage = messageByMessageType["operationtimedout"].Last();
                                    var operationTimedoutData = operationTimedoutMessage.Event.Data.ParseJson<IDictionary<string, object>>();
                                    var operationCompletionMessageTypes = ((JArray)operationTimedoutData["operationCompletionMessageTypes"]).ToObject<string[]>();
                                    var completionMessageType = messageByMessageType.Select(x1 => x1.Key).FirstOrDefault(operationCompletionMessageTypes.Contains);
                                    if (Equals(completionMessageType, default(string)))
                                    {
                                        return;
                                    }
                                    var callbackClientMessage = messageByMessageType["callbackclient"].Last();
                                    var callbackClientData = callbackClientMessage.Event.Data.ParseJson<IDictionary<string, object>>();
                                    var scriptId = callbackClientData["scriptId"];
                                    var expectedVersion = (long)callbackClientData["expectedVersion"];
                                    var streamName = $"message-{scriptId}";
                                    var completionMessage = messageByMessageType[completionMessageType].Last();
                                    try
                                    {
                                        await connection.AppendToStreamAsync(
                                            streamName, 
                                            expectedVersion,
                                            new[]
                                            {
                                                new EventData(Guid.NewGuid(), "pushresulttoclient", false, 
                                                    new Dictionary<string, object>
                                                    {
                                                        { "clientUri", callbackClientData["clientUri"] },
                                                        { "resultUri", $"{(string)operationTimedoutData["baseResultUri"]}/{completionMessage.Event.EventId}" }
                                                    }.ToJsonBytes(), 
                                                    x.Event.Metadata)
                                            },
                                            new UserCredentials(EventStoreSettings.Username, EventStoreSettings.Password)
                                        );
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
