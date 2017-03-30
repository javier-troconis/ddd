using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using EventStore.ClientAPI;

namespace eventstore
{
	public interface IPersistentSubscriptionManager
	{
		Task CreateOrUpdatePersistentSubscription(string streamName, string groupName, PersistentSubscriptionSettingsBuilder persistentSubscriptionSettings);
	}

	public class PersistentSubscriptionManager : IPersistentSubscriptionManager
	{
		private readonly Func<IEventStoreConnection> _createConnection;

		public PersistentSubscriptionManager(Func<IEventStoreConnection> createConnection)
		{
			_createConnection = createConnection;
		}

		public async Task CreateOrUpdatePersistentSubscription(string streamName, string groupName, PersistentSubscriptionSettingsBuilder persistentSubscriptionSettings)
		{
			using (var connection = _createConnection())
			{
				await connection.ConnectAsync();
				try
				{
					await connection.CreatePersistentSubscriptionAsync(streamName, groupName, persistentSubscriptionSettings, connection.Settings.DefaultUserCredentials);
				}
				catch (InvalidOperationException)
				{
					await connection.UpdatePersistentSubscriptionAsync(streamName, groupName, persistentSubscriptionSettings, connection.Settings.DefaultUserCredentials);
				}
			}
		}
	}
}
