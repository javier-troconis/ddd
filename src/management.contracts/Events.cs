using System;

namespace management.contracts
{
	public interface IStopSubscription
	{
		string SubscriptionName { get; }
	}

	public interface ISubscriptionStopped
	{
		string SubscriptionName { get; }
	}

	public interface IStartSubscription
	{
		string SubscriptionName { get; }
	}

	public interface ISubscriptionStarted
	{
		string SubscriptionName { get; }
	}

	//public interface IStartWorkflow<TWorkflow>
	//{
	//	Guid WorkflowId { get; }
	//}

	public interface IStartReconnectSubscriberWorkflow
	{
		Guid WorkflowId { get; }
	}
}
