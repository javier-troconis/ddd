

using shared;

namespace eventstore
{
	[Topic]
	public interface IProvisionSubscriptionStreamRequested
	{
		string SubscriptionStream { get; }
	}

	public class ProvisionSubscriptionStreamRequested : IProvisionSubscriptionStreamRequested
	{
	    public ProvisionSubscriptionStreamRequested(string subscriptionStream)
	    {
		    SubscriptionStream = subscriptionStream;
	    }

	    public string SubscriptionStream { get; }
	}
}
