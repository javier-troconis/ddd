using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using EventStore.ClientAPI;

namespace eventstore
{
	public interface IPersistentSubscriptionManager
	{
		Task CreateOrUpdatePersistentSubscription(string streamName, string groupName, PersistentSubscriptionSettings subscriptionSettings);
	}

	public class PersistentSubscriptionManager : IPersistentSubscriptionManager
	{
		private readonly Func<IEventStoreConnection> _createConnection;

		public PersistentSubscriptionManager(Func<IEventStoreConnection> createConnection)
		{
			_createConnection = createConnection;
		}

		public async Task CreateOrUpdatePersistentSubscription(string streamName, string groupName, PersistentSubscriptionSettings subscriptionSettings)
		{
            using (var connection = _createConnection())
			{
				await connection.ConnectAsync();
				try
				{
					await connection.CreatePersistentSubscriptionAsync(streamName, groupName, subscriptionSettings, connection.Settings.DefaultUserCredentials);
				}
				catch (InvalidOperationException)
				{
					await connection.UpdatePersistentSubscriptionAsync(streamName, groupName, subscriptionSettings, connection.Settings.DefaultUserCredentials);
				}
			}
		}
	}
}
