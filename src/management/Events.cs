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
		public StartProvisionSubscriptionStreamScript(string subscriptionName, string subscriberName)
		{
			SubscriptionStreamName = subscriptionName;
			SubscriberName = subscriberName;
		}

		public string SubscriptionStreamName { get; }
		public string SubscriberName { get; }
	}
}
