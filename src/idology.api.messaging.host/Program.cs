using System;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using System.Threading.Tasks;
using System.Collections.Generic;
using eventstore;

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
                
                var rnd = new Random();
                
                var eventBus = EventBus.CreateEventBus
                (
                    CreateConnection,
                    registry => registry.RegisterPersistentSubscriber("verifyidentity", "$et-verifyidentity", "verifyidentity", 
                    x => 
                        connection.AppendToStreamAsync($"message-{Guid.NewGuid():N}", ExpectedVersion.NoStream,
                            new[]
                            {
                                new EventData(Guid.NewGuid(), "verifyidentitysucceeded", false, new byte[0],
                                    new Dictionary<string, string>
                                    {
                                        {
                                            EventHeaderKey.CorrelationId, x.Event.Metadata.ParseJson<IDictionary<string, string>>()[EventHeaderKey.CorrelationId]
                                        }
                                    }.ToJsonBytes()
                                )
                            },
                            new UserCredentials(EventStoreSettings.Username, EventStoreSettings.Password)
                    ))
                );

                await connection.ConnectAsync();
                await eventBus.StartAllSubscribers();
            });

            while (true) { }
            
        }
    }
}
