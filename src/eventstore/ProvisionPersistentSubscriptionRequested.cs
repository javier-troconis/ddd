

using shared;

namespace eventstore
{
	[Topic]
	public interface IProvisionPersistentSubscriptionRequested
	{
		string PersistentSubscriptionGroup { get; }
	}


	public class ProvisionPersistentSubscriptionRequested : IProvisionPersistentSubscriptionRequested
	{
	    public ProvisionPersistentSubscriptionRequested(string persistentSubscriptionGroup)
	    {
		    PersistentSubscriptionGroup = persistentSubscriptionGroup;
	    }

	    public string PersistentSubscriptionGroup { get; }
	}
}
