using System;
using System.Threading.Tasks;

using EventStore.ClientAPI;

using shared;

namespace eventstore
{
	public interface IPersistentSubscriptionProvisioner
	{
		Task ProvisionPersistentSubscription<TSubscription>(
			Func<PersistentSubscriptionSettingsBuilder, PersistentSubscriptionSettingsBuilder> configurePersistentSubscription = null) where TSubscription : IMessageHandler;
		Task ProvisionPersistentSubscription<TSubscription, TSubscriptionGroup>(
			Func<PersistentSubscriptionSettingsBuilder, PersistentSubscriptionSettingsBuilder> configurePersistentSubscription = null) where TSubscription : IMessageHandler where TSubscriptionGroup : TSubscription;
	}

	public class PersistentSubscriptionProvisioner : IPersistentSubscriptionProvisioner
	{
		private readonly IPersistentSubscriptionManager _persistentSubscriptionManager;

		public PersistentSubscriptionProvisioner(IPersistentSubscriptionManager persistentSubscriptionManager)
		{
			_persistentSubscriptionManager = persistentSubscriptionManager;
		}

		public Task ProvisionPersistentSubscription<TSubscription>(
			Func<PersistentSubscriptionSettingsBuilder, PersistentSubscriptionSettingsBuilder> configurePersistentSubscription = null) where TSubscription : IMessageHandler
		{
			return ProvisionPersistentSubscription<TSubscription, TSubscription>(configurePersistentSubscription);
		}

		public Task ProvisionPersistentSubscription<TSubscription, TSubscriptionGroup>(
			Func<PersistentSubscriptionSettingsBuilder, PersistentSubscriptionSettingsBuilder> configurePersistentSubscription = null) where TSubscription : IMessageHandler where TSubscriptionGroup : TSubscription
		{
			configurePersistentSubscription = configurePersistentSubscription ?? (x => x);
			var streamName = typeof(TSubscription).GetEventStoreName();
			var groupName = typeof(TSubscriptionGroup).GetEventStoreName();
			var persistentSubscriptionSettings = configurePersistentSubscription(
				PersistentSubscriptionSettings
					.Create()
					.ResolveLinkTos()
					//.StartFromBeginning()
					.StartFromCurrent()
					//.MinimumCheckPointCountOf(5)
					//.MaximumCheckPointCountOf(10)
					.MaximumCheckPointCountOf(1)
					.CheckPointAfter(TimeSpan.FromSeconds(1))
					.WithExtraStatistics());
			return _persistentSubscriptionManager.CreateOrUpdatePersistentSubscription(streamName, groupName, persistentSubscriptionSettings);
		}
	}

}
