using System;

namespace management.contracts
{
	//public interface IStartWorkflow<TWorkflow>
	//{
	//	Guid WorkflowId { get; }
	//}

	public interface IStartRestartSubscriberWorkflow2
	{
		Guid WorkflowId { get; }
		string SubscriberName { get; }
	}

	public interface IStartRestartSubscriberWorkflow1
	{
		Guid WorkflowId { get; }
		string SubscriberName { get; }
	}
}
