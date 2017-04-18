

namespace eventstore
{
	public interface IPersistentSubscriptionsProvisioningRequested
	{
		string PersistentSubscriptionName { get; }
	}

	internal class PersistentSubscriptionsProvisioningRequested : IPersistentSubscriptionsProvisioningRequested
	{
	    public PersistentSubscriptionsProvisioningRequested(string persistentSubscriptionName)
	    {
		    PersistentSubscriptionName = persistentSubscriptionName;
	    }

	    public string PersistentSubscriptionName { get; }
	}
}
