using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using management.contracts;

namespace management
{
	internal struct StartProvisionSubscriptionStreamScript : IStartProvisionSubscriptionStreamScript
	{
		public StartProvisionSubscriptionStreamScript(Guid workflowId, string subscriptionName, string subscriberName)
		{
			WorkflowId = workflowId;
			SubscriptionStreamName = subscriptionName;
			SubscriberName = subscriberName;
		}

		public Guid WorkflowId { get; }
		public string SubscriptionStreamName { get; }
		public string SubscriberName { get; }
	}
}
