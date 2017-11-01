using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using management.contracts;

namespace management
{
	internal struct StartProvisionSubscriptionStreamWorkflow : IStartProvisionSubscriptionStreamWorkflow
	{
		public StartProvisionSubscriptionStreamWorkflow(Guid workflowId, string subscriptionName, string subscriberName)
		{
			WorkflowId = workflowId;
			SubscriptionName = subscriptionName;
			SubscriberName = subscriberName;
		}

		public Guid WorkflowId { get; }
		public string SubscriptionName { get; }
		public string SubscriberName { get; }
	}

	internal struct StartRestartSubscriberWorkflow : IStartRestartSubscriberWorkflow
	{
		public StartRestartSubscriberWorkflow(Guid workflowId, string subscriberName)
		{
			WorkflowId = workflowId;
			SubscriberName = subscriberName;
		}

		public Guid WorkflowId { get; }
		public string SubscriberName { get; }
	}
}
