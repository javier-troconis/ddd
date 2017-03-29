using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using EventStore.ClientAPI;

namespace eventstore
{
	public interface IPersistentSubscriptionRegistry
	{
		Task RegisterPersistentSubscription<TStreamName, TGroupName>(Action<PersistentSubscriptionSettingsBuilder> configurePersistentSubscription = null);
	}

    public class PersistentSubscriptionRegistry : IPersistentSubscriptionRegistry
	{
	    private readonly IPersistentSubscriptionManager _persistentSubscriptionManager;

	    public PersistentSubscriptionRegistry(IPersistentSubscriptionManager persistentSubscriptionManager)
	    {
		    _persistentSubscriptionManager = persistentSubscriptionManager;
	    }

	    public Task RegisterPersistentSubscription<TStreamName, TGroupName>(Action<PersistentSubscriptionSettingsBuilder> configurePersistentSubscription)
	    {
		    var streamName = typeof(TStreamName).GetEventStoreName();
		    var groupName = typeof(TGroupName).GetEventStoreName();
		    return _persistentSubscriptionManager.CreateOrUpdatePersistentSubscription(streamName, groupName, configurePersistentSubscription);
	    }
	}
}
