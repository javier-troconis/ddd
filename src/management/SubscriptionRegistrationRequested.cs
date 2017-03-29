using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using management.contracts;

namespace management
{
    // register subscription projections - persistent subscriber
    public class SubscriptionRegistrationRequested : ISubscriptionRegistrationRequested
	{
		public SubscriptionRegistrationRequested(string serviceName, string subscriptionName)
		{
			ServiceName = serviceName;
			SubscriptionName = subscriptionName;
		}

		public string ServiceName { get; }
		public string SubscriptionName { get; }
	}

    // register persistent subscriptions - volatile subscriber
}
