﻿using System;
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
        IPersistentSubscriptionProvisioner RegisterPersistentSubscriptionProvisioning<TSubscription>(
            Func<PersistentSubscriptionSettingsBuilder, PersistentSubscriptionSettingsBuilder> configurePersistentSubscription = null) where TSubscription : IMessageHandler;

        IPersistentSubscriptionProvisioner RegisterPersistentSubscriptionProvisioning<TSubscription, TSubscriptionGroup>(
            Func<PersistentSubscriptionSettingsBuilder, PersistentSubscriptionSettingsBuilder> configurePersistentSubscription = null) where TSubscription : IMessageHandler where TSubscriptionGroup : TSubscription;

        Task ProvisionPersistentSubscriptions(string targetSubscriptionGroupName = "*");
    }

    public class PersistentSubscriptionProvisioner : IPersistentSubscriptionProvisioner
    {
        private static readonly TaskQueue _provisioningTasksQueue = new TaskQueue();
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

        public IPersistentSubscriptionProvisioner RegisterPersistentSubscriptionProvisioning<TSubscription>(
            Func<PersistentSubscriptionSettingsBuilder, PersistentSubscriptionSettingsBuilder> configurePersistentSubscription = null) where TSubscription : IMessageHandler
        {
            return RegisterPersistentSubscriptionProvisioning<TSubscription, TSubscription>(configurePersistentSubscription);
        }

        public IPersistentSubscriptionProvisioner RegisterPersistentSubscriptionProvisioning<TSubscription, TSubscriptionGroup>(
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
                                configurePersistentSubscription = configurePersistentSubscription ?? (x => x);
                                var persistentSubscriptionSettings = configurePersistentSubscription(
                                    PersistentSubscriptionSettings
                                        .Create()
                                        .ResolveLinkTos()
                                        .StartFromBeginning()
                                        .MaximumCheckPointCountOf(1)
                                        .CheckPointAfter(TimeSpan.FromSeconds(1))
                                        .WithExtraStatistics()
                                    );
                                return _provisioningTasksQueue.SendToChannel(streamName,
                                    () =>_persistentSubscriptionManager.CreateOrUpdatePersistentSubscription(streamName, subscriptionGroupName, persistentSubscriptionSettings));
                            }
                    })
            );
        }

        public Task ProvisionPersistentSubscriptions(string targetSubscriptionGroupName)
        {
            return Task.WhenAll(_provisioningTasks.Select(x => x(targetSubscriptionGroupName)));
        }
    }
}
