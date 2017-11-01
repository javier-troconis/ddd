

namespace eventstore
{

    public interface IProvisionSubscriptionStream
	{
		string SubscriptionStream { get; }
	}

	public class ProvisionSubscriptionStream : IProvisionSubscriptionStream
	{
	    public ProvisionSubscriptionStream(string subscriptionStream)
	    {
		    SubscriptionStream = subscriptionStream;
	    }

	    public string SubscriptionStream { get; }
	}
}
