using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using EventStore.ClientAPI;

using ImpromptuInterface.InvokeExt;
using shared;

namespace eventstore
{
    public interface IPersistentSubscriptionProvisioner
    {
        IPersistentSubscriptionProvisioner RegisterPersistentSubscription<TSubscription>(
            Func<PersistentSubscriptionSettingsBuilder, PersistentSubscriptionSettingsBuilder> configurePersistentSubscription = null) where TSubscription : IMessageHandler;

        IPersistentSubscriptionProvisioner RegisterPersistentSubscription<TSubscription, TSubscriptionGroup>(
            Func<PersistentSubscriptionSettingsBuilder, PersistentSubscriptionSettingsBuilder> configurePersistentSubscription = null) where TSubscription : IMessageHandler where TSubscriptionGroup : TSubscription;

        Task ProvisionPersistentSubscription(string targetSubscriptionGroupName = "*");
    }

    public class PersistentSubscriptionProvisioner : IPersistentSubscriptionProvisioner
    {
        private readonly IPersistentSubscriptionManager _persistentSubscriptionManager;
        private readonly IEnumerable<Func<string, Task>> _provisioningTasks;

        public PersistentSubscriptionProvisioner(IPersistentSubscriptionManager persistentSubscriptionManager)
            :
                this(persistentSubscriptionManager, Enumerable.Empty<Func<string, Task>>())
        {
        }

        private PersistentSubscriptionProvisioner(
            IPersistentSubscriptionManager persistentSubscriptionManager,
            IEnumerable<Func<string, Task>> provisioningTasks
            )
        {
            _persistentSubscriptionManager = persistentSubscriptionManager;
            _provisioningTasks = provisioningTasks;
        }

        public IPersistentSubscriptionProvisioner RegisterPersistentSubscription<TSubscription>(
            Func<PersistentSubscriptionSettingsBuilder, PersistentSubscriptionSettingsBuilder> configurePersistentSubscription = null) where TSubscription : IMessageHandler
        {
            return RegisterPersistentSubscription<TSubscription, TSubscription>(configurePersistentSubscription);
        }

        public IPersistentSubscriptionProvisioner RegisterPersistentSubscription<TSubscription, TSubscriptionGroup>(
            Func<PersistentSubscriptionSettingsBuilder, PersistentSubscriptionSettingsBuilder> configurePersistentSubscription = null) where TSubscription : IMessageHandler where TSubscriptionGroup : TSubscription
        {
            return new PersistentSubscriptionProvisioner(
                _persistentSubscriptionManager,
                _provisioningTasks.Concat(
                    new Func<string, Task>[] {
                        targetSubscriptionGroupName =>
                            {
                                var subscriptionGroupName = typeof(TSubscriptionGroup).GetEventStoreName();
                                if(!subscriptionGroupName.MatchesWildcard(targetSubscriptionGroupName))
                                {
                                    return Task.CompletedTask;
                                }
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
                                return _persistentSubscriptionManager.CreateOrUpdatePersistentSubscription(streamName, subscriptionGroupName, persistentSubscriptionSettings);
                            }
                    })
            );
        }

        public Task ProvisionPersistentSubscription(string targetSubscriptionGroupName)
        {
            return Task.WhenAll(_provisioningTasks.Select(x => x(targetSubscriptionGroupName)));
        }
    }
}
