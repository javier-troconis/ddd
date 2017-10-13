

namespace eventstore
{
	public interface IProvisionPersistentSubscriptionRequested
	{
		string PersistentSubscriptionGroup { get; }
	}

	internal class ProvisionPersistentSubscriptionRequested : IProvisionPersistentSubscriptionRequested
	{
	    public ProvisionPersistentSubscriptionRequested(string persistentSubscriptionGroup)
	    {
		    PersistentSubscriptionGroup = persistentSubscriptionGroup;
	    }

	    public string PersistentSubscriptionGroup { get; }
	}
}
