using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using management.contracts;

namespace management
{
    internal struct StartSubscription : IStartSubscription 
    {
	    public StartSubscription(string subscriptionName)
	    {
		    SubscriptionName = subscriptionName;
	    }

	    public string SubscriptionName { get; }
    }

	internal struct StopSubscription : IStopSubscription
	{
		public StopSubscription(string subscriptionName)
		{
			SubscriptionName = subscriptionName;
		}

		public string SubscriptionName { get; }
	}

	internal struct StartReconnectSubscriberWorkflow : IStartReconnectSubscriberWorkflow
	{

	}

	internal struct WorkflowStepExecuted : IWorkflowStepExecuted
	{
		public WorkflowStepExecuted(string workflowStepName)
		{
			WorkflowStepName = workflowStepName;
		}

		public string WorkflowStepName { get; }
	}
}
