using System;

namespace management.contracts
{
	public interface IRunProvisionSubscriptionStreamWorkflow
	{
		Guid WorkflowId { get; }
		string SubscriptionStreamName { get; }
		string SubscriberName { get; }
	}

	public interface IRunRestartSubscriberWorkflow
	{
		Guid WorkflowId { get; }
		string SubscriberName { get; }
	}
}
