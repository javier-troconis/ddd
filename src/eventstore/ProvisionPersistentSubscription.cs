

namespace eventstore
{
	public interface IProvisionPersistentSubscription
	{
		string SubscriptionStreamName { get; }
		string PersistentSubscriptionGroupName { get; }
	}

	public class ProvisionPersistentSubscription : IProvisionPersistentSubscription
	{
	    public ProvisionPersistentSubscription(string subscriptionStreamName, string persistentSubscriptionGroupName)
	    {
		    SubscriptionStreamName = subscriptionStreamName;
		    PersistentSubscriptionGroupName = persistentSubscriptionGroupName;
	    }

		public string SubscriptionStreamName { get; }
		public string PersistentSubscriptionGroupName { get; }
	}
}
