using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;

namespace infra
{
    public class PersistentSubscriptionManager
    {
        private readonly Func<IEventStoreConnection> _createConnection;
	    private readonly string _username;
	    private readonly string _password;

	    public PersistentSubscriptionManager(Func<IEventStoreConnection> createConnection, string username, string password)
	    {
		    _createConnection = createConnection;
		    _username = username;
		    _password = password;
	    }

	    public async Task CreatePersistentSubscription(string streamName, string groupName)
        {
            var subscriptionSettings = PersistentSubscriptionSettings.Create()
                .ResolveLinkTos()
                .StartFromCurrent()
				.MinimumCheckPointCountOf(1)
				.MaximumCheckPointCountOf(1)
				.CheckPointAfter(TimeSpan.FromSeconds(1))
				.WithExtraStatistics();
            using (var connection = _createConnection())
            {
                await connection.ConnectAsync();
                await connection.CreatePersistentSubscriptionAsync(streamName, groupName, subscriptionSettings, new UserCredentials(_username, _password));
            }
        }
    }
}
