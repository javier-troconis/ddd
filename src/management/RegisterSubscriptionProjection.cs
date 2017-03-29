using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using management.contracts;

namespace management
{
    public struct RegisterSubscriptionProjection : IRegisterSubscriptionProjection
    {
	    public RegisterSubscriptionProjection(string serviceName, string subscriptionName)
	    {
		    ServiceName = serviceName;
		    SubscriptionName = subscriptionName;
	    }

	    public string ServiceName { get; }
	    public string SubscriptionName { get; }
    }
}
