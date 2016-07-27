using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;

namespace infra
{
    public class ConsumerGroupManager
    {
        private readonly EventStoreConnectionFactory _connectionFactory;

        public ConsumerGroupManager(EventStoreConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task EnsureConsumerAsync(UserCredentials userCredentials, string streamName, string consumerGroupName)
        {
            var subscriptionSettings = PersistentSubscriptionSettings.Create()
                .ResolveLinkTos()
                .StartFromCurrent()
                .WithExtraStatistics();
            using (var connection = _connectionFactory.Create())
            {
                await connection.ConnectAsync();
                try
                {
                    await connection.CreatePersistentSubscriptionAsync(streamName, consumerGroupName, subscriptionSettings, userCredentials);
                }
                catch (InvalidOperationException)
                {

                }
            }
        }
    }
}
