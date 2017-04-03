using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using EventStore.ClientAPI;

namespace eventstore
{
	public interface IPersistentSubscriptionManager
	{
		Task CreateOrUpdatePersistentSubscription(string streamName, string groupName, Func<PersistentSubscriptionSettingsBuilder, PersistentSubscriptionSettingsBuilder> configurePersistentSubscription = null);
	}

	public class PersistentSubscriptionManager : IPersistentSubscriptionManager
	{
		private readonly Func<IEventStoreConnection> _createConnection;

		public PersistentSubscriptionManager(Func<IEventStoreConnection> createConnection)
		{
			_createConnection = createConnection;
		}

		public async Task CreateOrUpdatePersistentSubscription(string streamName, string groupName, Func<PersistentSubscriptionSettingsBuilder, PersistentSubscriptionSettingsBuilder> configurePersistentSubscription)
		{
            var persistentSubscriptionSettings = (configurePersistentSubscription ?? (x => x))(
                PersistentSubscriptionSettings.Create()
                .ResolveLinkTos()
                .StartFromCurrent()
                .MinimumCheckPointCountOf(5)
                .MaximumCheckPointCountOf(10)
                .CheckPointAfter(TimeSpan.FromSeconds(1))
                .WithExtraStatistics());

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
