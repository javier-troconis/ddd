

namespace eventstore
{
	public interface ISubscriptionStreamsProvisioningRequested
	{
		string SubscriptionStreamName { get; }
	}

	internal class SubscriptionStreamsProvisioningRequested : ISubscriptionStreamsProvisioningRequested
	{
	    public SubscriptionStreamsProvisioningRequested(string subscriptionStreamName)
	    {
		    SubscriptionStreamName = subscriptionStreamName;
	    }

	    public string SubscriptionStreamName { get; }
	}
}
