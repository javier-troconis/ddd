﻿using System;

namespace management.contracts
{
	public interface IStopSubscriber
	{
		string SubscriberName { get; }
	}

	public interface ISubscriberStopped
	{
		string SubscriberName { get; }
	}

	public interface IStartSubscriber
	{
		string SubscriberName { get; }
	}

	public interface ISubscriberStarted
	{
		string SubscriberName { get; }
	}

	//public interface IStartWorkflow<TWorkflow>
	//{
	//	Guid WorkflowId { get; }
	//}

	public interface IStartReconnectSubscriberWorkflow
	{
		Guid WorkflowId { get; }
		string SubscriberName { get; }
	}
}