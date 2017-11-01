

namespace eventstore
{
	public interface IProvisionPersistentSubscription
	{
		string PersistentSubscriptionGroup { get; }
	}

	public class ProvisionPersistentSubscription : IProvisionPersistentSubscription
	{
	    public ProvisionPersistentSubscription(string persistentSubscriptionGroup)
	    {
		    PersistentSubscriptionGroup = persistentSubscriptionGroup;
	    }

	    public string PersistentSubscriptionGroup { get; }
	}
}
