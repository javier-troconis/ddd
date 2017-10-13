

namespace eventstore
{

    public interface IProvisionSubscriptionStreamRequested
	{
		string SubscriptionStream { get; }
	}

	internal class ProvisionSubscriptionStreamRequested : IProvisionSubscriptionStreamRequested
	{
	    public ProvisionSubscriptionStreamRequested(string subscriptionStream)
	    {
		    SubscriptionStream = subscriptionStream;
	    }

	    public string SubscriptionStream { get; }
	}
}
