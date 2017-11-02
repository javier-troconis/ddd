

namespace eventstore
{

    public interface IProvisionSubscriptionStream
	{
		string SubscriptionStreamName { get; }
	}

	public class ProvisionSubscriptionStream : IProvisionSubscriptionStream
	{
	    public ProvisionSubscriptionStream(string subscriptionStreamName)
	    {
		    SubscriptionStreamName = subscriptionStreamName;
	    }

	    public string SubscriptionStreamName { get; }
	}
}
