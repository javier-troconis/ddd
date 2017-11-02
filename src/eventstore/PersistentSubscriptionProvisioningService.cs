using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using EventStore.ClientAPI;

using ImpromptuInterface.InvokeExt;
using shared;

namespace eventstore
{
	public enum ProvisionPersistentSubscriptionResult
	{
		NotFound,
		Provisioned
	}

	// todo:change this to follow the same pattern as the eventbus/subscriber registry
	public interface IPersistentSubscriptionProvisioningService
    {
        IPersistentSubscriptionProvisioningService RegisterPersistentSubscription<TSubscription>(
            Func<PersistentSubscriptionSettingsBuilder, PersistentSubscriptionSettingsBuilder> configurePersistentSubscription = null) where TSubscription : IMessageHandler;

        IPersistentSubscriptionProvisioningService RegisterPersistentSubscription<TSubscription, TSubscriptionGroup>(
            Func<PersistentSubscriptionSettingsBuilder, PersistentSubscriptionSettingsBuilder> configurePersistentSubscription = null) where TSubscription : IMessageHandler where TSubscriptionGroup : TSubscription;

        Task ProvisionAllPersistentSubscriptions();

	    Task<ProvisionPersistentSubscriptionResult> ProvisionPersistentSubscription(string subscriptionStreamName, string subscriptionGroupName);
	}

    public class PersistentSubscriptionProvisioningService : IPersistentSubscriptionProvisioningService
    {
        private readonly IPersistentSubscriptionManager _persistentSubscriptionManager;
        private readonly IDictionary<string, Func<Task>> _registry;

        public PersistentSubscriptionProvisioningService
			(
				IPersistentSubscriptionManager persistentSubscriptionManager
			) : this
			(
					persistentSubscriptionManager, 
					new Dictionary<string, Func<Task>>()
			)
        {
        }

        private PersistentSubscriptionProvisioningService(
            IPersistentSubscriptionManager persistentSubscriptionManager,
            IDictionary<string, Func<Task>> registry
            )
        {
            _persistentSubscriptionManager = persistentSubscriptionManager;
            _registry = registry;
        }

        public IPersistentSubscriptionProvisioningService RegisterPersistentSubscription<TSubscription>(
            Func<PersistentSubscriptionSettingsBuilder, PersistentSubscriptionSettingsBuilder> configurePersistentSubscription = null) where TSubscription : IMessageHandler
        {
            return RegisterPersistentSubscription<TSubscription, TSubscription>(configurePersistentSubscription);
        }

        public IPersistentSubscriptionProvisioningService RegisterPersistentSubscription<TSubscription, TSubscriptionGroup>(
            Func<PersistentSubscriptionSettingsBuilder, PersistentSubscriptionSettingsBuilder> configurePersistentSubscription = null) where TSubscription : IMessageHandler where TSubscriptionGroup : TSubscription
        {
            return new PersistentSubscriptionProvisioningService
				(
					_persistentSubscriptionManager,
					new Dictionary<string, Func<Task>>
					{
						{
							typeof(TSubscription).GetEventStoreName() + "-" + typeof(TSubscriptionGroup).GetEventStoreName(),
							() =>
							{
								var streamName = typeof(TSubscription).GetEventStoreName();
								var persistentSubscriptionSettings = (configurePersistentSubscription ?? (x => x))(
									PersistentSubscriptionSettings
										.Create()
										.ResolveLinkTos()
										.StartFromBeginning()
										.MaximumCheckPointCountOf(1)
										.CheckPointAfter(TimeSpan.FromSeconds(1))
										.WithExtraStatistics()
									);
								return _persistentSubscriptionManager.CreateOrUpdatePersistentSubscription(streamName, typeof(TSubscriptionGroup).GetEventStoreName(), persistentSubscriptionSettings);
							}
						}
					}.Merge(_registry)
				);
        }

	    public Task ProvisionAllPersistentSubscriptions()
	    {
			return Task.WhenAll(
				_registry.Select(
					x =>
					{
						var parts = x.Key.Split('-');
						return ProvisionPersistentSubscription(parts[0], parts[1]);
					}));
		}

	    public async Task<ProvisionPersistentSubscriptionResult> ProvisionPersistentSubscription(string subscriptionStreamName, string subscriptionGroupName)
	    {
			if (!_registry.TryGetValue(subscriptionStreamName + "-" + subscriptionGroupName, out Func<Task> operation))
		    {
			    return ProvisionPersistentSubscriptionResult.NotFound;
		    }
		    await operation();
		    return ProvisionPersistentSubscriptionResult.Provisioned;
		}

	   
    }
}
