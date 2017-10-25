using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using management.contracts;

namespace management
{
	internal struct StartRestartSubscriberWorkflow2 : IStartRestartSubscriberWorkflow2
	{
		public StartRestartSubscriberWorkflow2(Guid workflowId, string subscriberName)
		{
			WorkflowId = workflowId;
			SubscriberName = subscriberName;
		}

		public Guid WorkflowId { get; }
		public string SubscriberName { get; }
	}

	internal struct StartRestartSubscriberWorkflow1 : IStartRestartSubscriberWorkflow1
	{
		public StartRestartSubscriberWorkflow1(Guid workflowId, string subscriberName)
		{
			WorkflowId = workflowId;
			SubscriberName = subscriberName;
		}

		public Guid WorkflowId { get; }
		public string SubscriberName { get; }
	}
}
