using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using management.contracts;

namespace management
{
    public struct SubscriptionStreamRegistrationRequested : ISubscriptionStreamRegistrationRequested
    {
	    public SubscriptionStreamRegistrationRequested(string serviceName, string subscriptionStreamName)
	    {
		    ServiceName = serviceName;
		    SubscriptionStreamName = subscriptionStreamName;
	    }

	    public string ServiceName { get; }
	    public string SubscriptionStreamName { get; }
    }
}
