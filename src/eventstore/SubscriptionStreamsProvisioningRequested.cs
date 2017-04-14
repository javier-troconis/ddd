

namespace eventstore
{
	public interface ISubscriptionStreamsProvisioningRequested
	{
		string ServiceName { get; }
		string SubscriptionStreamName { get; }
	}

	internal class SubscriptionStreamsProvisioningRequested : ISubscriptionStreamsProvisioningRequested
	{
	    public SubscriptionStreamsProvisioningRequested(string serviceName, string subscriptionStreamName)
	    {
		    ServiceName = serviceName;
		    SubscriptionStreamName = subscriptionStreamName;
	    }

	    public string ServiceName { get; }
	    public string SubscriptionStreamName { get; }
	}
}
