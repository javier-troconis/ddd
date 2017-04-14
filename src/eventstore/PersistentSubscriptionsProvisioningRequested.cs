

namespace eventstore
{
	public interface IPersistentSubscriptionsProvisioningRequested
	{
		string ServiceName { get; }
		string PersistentSubscriptionName { get; }
	}

	internal class PersistentSubscriptionsProvisioningRequested : IPersistentSubscriptionsProvisioningRequested
	{
	    public PersistentSubscriptionsProvisioningRequested(string serviceName, string persistentSubscriptionName)
	    {
		    ServiceName = serviceName;
		    PersistentSubscriptionName = persistentSubscriptionName;
	    }

	    public string ServiceName { get; }
	    public string PersistentSubscriptionName { get; }
	}
}
