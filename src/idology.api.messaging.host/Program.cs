using System;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using eventstore;
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
                                            new EventData(eventId, "verifyidentitysucceeded", false, new byte[0],
                                                new Dictionary<string, object>
                                                {
                                                    {
                                                        EventHeaderKey.CorrelationId,
                                                        x.Event.Metadata.ParseJson<IDictionary<string, object>>()[
                                                            EventHeaderKey.CorrelationId]
                                                    },
                                                    {
                                                        EventHeaderKey.CausationId, x.Event.EventId
                                                    }
                                                }.ToJsonBytes()
                                            )
                                        },
                                        new UserCredentials(EventStoreSettings.Username, EventStoreSettings.Password)
                                    );
                                })
                            .RegisterPersistentSubscriber("callback", "$ce-message", "callback",
                                async x =>
                                {
                                    var events = await connection.ReadStreamEventsForwardAsync($"$bc-{x.Event.Metadata.ParseJson<IDictionary<string, object>>()[EventHeaderKey.CorrelationId]}", 0, 4096, true,
                                        new UserCredentials(EventStoreSettings.Username, EventStoreSettings.Password));
                                    var messageByMessageType = events.Events.ToDictionary(y => y.Event.EventType, y => y.Event);
                                    if (messageByMessageType.ContainsKey("callbacksucceeded") || !messageByMessageType.TryGetValue("tasktimedout", out var taskTimedOut))
                                    {
                                        return;
                                    }
                                    var data = taskTimedOut.Data.ParseJson<IDictionary<string, object>>();
                                    if (!data.TryGetValue("callbackUri", out var callbackUri))
                                    {
                                        return;
                                    }
                                    var taskCompletionMessageTypes = ((JArray)data["taskCompletionMessageTypes"]).ToObject<string[]>();
                                    var completionMessageType = messageByMessageType.Select(x1 => x1.Key).FirstOrDefault(taskCompletionMessageTypes.Contains);
                                    if (Equals(completionMessageType, default(string)))
                                    {
                                        return;
                                    }

                                    var client = new HttpClient();
                                    var request = new HttpRequestMessage(HttpMethod.Post, (string) callbackUri)
                                    {
                                        Content = new StringContent(
                                            (string) data["resultUri"] + "/" +
                                            messageByMessageType[completionMessageType].EventId)
                                    };
                                    await client.SendAsync(request);
                                    var eventId = Guid.NewGuid();
                                    await connection.AppendToStreamAsync($"message-{eventId}", ExpectedVersion.NoStream,
                                        new[]
                                        {
                                            new EventData(eventId, "callbacksucceeded", false, new byte[0],
                                                new Dictionary<string, object>
                                                {
                                                    {
                                                        EventHeaderKey.CorrelationId,
                                                        x.Event.Metadata.ParseJson<IDictionary<string, object>>()[
                                                            EventHeaderKey.CorrelationId]
                                                    },
                                                    {
                                                        EventHeaderKey.CausationId, x.Event.EventId
                                                    }
                                                }.ToJsonBytes()
                                            )
                                        },
                                        new UserCredentials(EventStoreSettings.Username, EventStoreSettings.Password)
                                    );
                                })

                );

                await connection.ConnectAsync();
                await eventBus.StartAllSubscribers();
            });

            while (true) { }
            
        }
    }
}
