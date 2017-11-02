using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using management.contracts;

namespace management
{
	internal struct RunProvisionSubscriptionStreamWorkflow : IRunProvisionSubscriptionStreamWorkflow
	{
		public RunProvisionSubscriptionStreamWorkflow(Guid workflowId, string subscriptionName, string subscriberName)
		{
			WorkflowId = workflowId;
			SubscriptionStreamName = subscriptionName;
			SubscriberName = subscriberName;
		}

		public Guid WorkflowId { get; }
		public string SubscriptionStreamName { get; }
		public string SubscriberName { get; }
	}

	internal struct RunRestartSubscriberWorkflow : IRunRestartSubscriberWorkflow
	{
		public RunRestartSubscriberWorkflow(Guid workflowId, string subscriberName)
		{
			WorkflowId = workflowId;
			SubscriberName = subscriberName;
		}

		public Guid WorkflowId { get; }
		public string SubscriberName { get; }
	}
}
