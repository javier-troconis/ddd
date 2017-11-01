

namespace eventstore
{
	public interface IProvisionPersistentSubscription
	{
		string SubscriptionStream { get; }
		string PersistentSubscriptionGroup { get; }
	}

	public class ProvisionPersistentSubscription : IProvisionPersistentSubscription
	{
	    public ProvisionPersistentSubscription(string subscriptionStream, string persistentSubscriptionGroup)
	    {
		    SubscriptionStream = subscriptionStream;
		    PersistentSubscriptionGroup = persistentSubscriptionGroup;
	    }

		public string SubscriptionStream { get; }
		public string PersistentSubscriptionGroup { get; }
	}
}
