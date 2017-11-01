using System;

namespace management.contracts
{
	public interface IStartProvisionSubscriptionStreamWorkflow
	{
		Guid WorkflowId { get; }
		string SubscriptionName { get; }
		string SubscriberName { get; }
	}

	public interface IStartRestartSubscriberWorkflow
	{
		Guid WorkflowId { get; }
		string SubscriberName { get; }
	}
}
