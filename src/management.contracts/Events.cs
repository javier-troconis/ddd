using System;

namespace management.contracts
{
	public interface IStartProvisionSubscriptionStreamScript
	{
		Guid WorkflowId { get; }
		string SubscriptionStreamName { get; }
		string SubscriberName { get; }
	}
}
