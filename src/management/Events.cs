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
		public StartReconnectSubscriberWorkflow(Guid workflowId)
		{
			WorkflowId = workflowId;
		}

		public Guid WorkflowId { get; }
	}
}
