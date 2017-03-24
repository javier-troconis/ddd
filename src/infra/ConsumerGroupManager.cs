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
        private readonly Func<IEventStoreConnection> _createConnection;

        public ConsumerGroupManager(Func<IEventStoreConnection> createConnection)
        {
			_createConnection = createConnection;
        }

        public async Task CreateConsumerGroup(UserCredentials userCredentials, string streamName, string consumerGroupName)
        {
            var subscriptionSettings = PersistentSubscriptionSettings.Create()
                .ResolveLinkTos()
                .StartFromCurrent()
				//.MinimumCheckPointCountOf(0)
				//.MaximumCheckPointCountOf(1)
				//.CheckPointAfter(TimeSpan.FromSeconds(1))
				.WithExtraStatistics();
            using (var connection = _createConnection())
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
