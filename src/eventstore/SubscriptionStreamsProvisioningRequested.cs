

namespace eventstore
{

    // todo: rename to ISubscriptionStreamProvisionRequested

    public interface ISubscriptionStreamsProvisioningRequested
	{
		string SubscriptionStream { get; }
	}

	internal class SubscriptionStreamsProvisioningRequested : ISubscriptionStreamsProvisioningRequested
	{
	    public SubscriptionStreamsProvisioningRequested(string subscriptionStream)
	    {
		    SubscriptionStream = subscriptionStream;
	    }

	    public string SubscriptionStream { get; }
	}
}
