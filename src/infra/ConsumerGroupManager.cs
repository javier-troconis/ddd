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
        private readonly IEventStoreConnection _connection;

        public ConsumerGroupManager(IEventStoreConnection connection)
        {
            _connection = connection;
        }

        public async Task EnsureConsumerAsync(UserCredentials userCredentials, string streamName, string consumerGroupName)
        {
            var subscriptionSettings = PersistentSubscriptionSettings.Create()
                .ResolveLinkTos()
                .StartFromCurrent()
                .WithExtraStatistics();
            try
            {
                await _connection.CreatePersistentSubscriptionAsync(streamName, consumerGroupName, subscriptionSettings, userCredentials);
            }
            catch (InvalidOperationException)
            {

            }
        }
    }
}
