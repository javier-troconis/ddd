using System;

namespace management.contracts
{
	public interface IStartProvisionSubscriptionStreamScript
	{
		string SubscriptionStreamName { get; }
		string SubscriberName { get; }
	}
}
