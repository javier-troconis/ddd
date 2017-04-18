

namespace eventstore
{
	public interface IPersistentSubscriptionsProvisioningRequested
	{
		string PersistentSubscriptionGroup { get; }
	}

	internal class PersistentSubscriptionsProvisioningRequested : IPersistentSubscriptionsProvisioningRequested
	{
	    public PersistentSubscriptionsProvisioningRequested(string persistentSubscriptionGroup)
	    {
		    PersistentSubscriptionGroup = persistentSubscriptionGroup;
	    }

	    public string PersistentSubscriptionGroup { get; }
	}
}
