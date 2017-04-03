using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using EventStore.ClientAPI;

namespace eventstore
{
	public interface IPersistentSubscriptionRegistry
	{
		Task RegisterPersistentSubscription<TStreamName, TGroupName>(Func<PersistentSubscriptionSettingsBuilder, PersistentSubscriptionSettingsBuilder> configurePersistentSubscription = null);
	}

	public class PersistentSubscriptionRegistry : IPersistentSubscriptionRegistry
	{
		private readonly IPersistentSubscriptionManager _persistentSubscriptionManager;

		public PersistentSubscriptionRegistry(IPersistentSubscriptionManager persistentSubscriptionManager)
		{
			_persistentSubscriptionManager = persistentSubscriptionManager;
		}

		public Task RegisterPersistentSubscription<TSubscription, TSubscriptionGroup>(Func<PersistentSubscriptionSettingsBuilder, PersistentSubscriptionSettingsBuilder> configurePersistentSubscription)
		{
			configurePersistentSubscription = configurePersistentSubscription ?? (x => x);
			var streamName = typeof(TSubscription).GetEventStoreName();
			var groupName = typeof(TSubscriptionGroup).GetEventStoreName();
			var persistentSubscriptionSettings = configurePersistentSubscription(
				PersistentSubscriptionSettings
					.Create()
					.ResolveLinkTos()
					.StartFromCurrent()
					.MinimumCheckPointCountOf(5)
					.MaximumCheckPointCountOf(10)
					.CheckPointAfter(TimeSpan.FromSeconds(1))
					.WithExtraStatistics());
			return _persistentSubscriptionManager.CreateOrUpdatePersistentSubscription(streamName, groupName, persistentSubscriptionSettings);
		}
	}
}