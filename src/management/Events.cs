using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using management.contracts;

namespace management
{
    internal struct StartSubscriber : IStartSubscriber 
    {
	    public StartSubscriber(string subscriberName)
	    {
		    SubscriberName = subscriberName;
	    }

	    public string SubscriberName { get; }
    }

	internal struct StopSubscriber : IStopSubscriber
	{
		public StopSubscriber(string subscriberName)
		{
			SubscriberName = subscriberName;
		}

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
