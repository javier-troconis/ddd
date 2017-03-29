using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using management.contracts;

namespace management
{
    public struct RegisterPersistentSubscription : IRegisterPersistentSubscription
	{
		public RegisterPersistentSubscription(string serviceName, string subscriptionName)
		{
			ServiceName = serviceName;
			SubscriptionName = subscriptionName;
		}

		public string ServiceName { get; }
		public string SubscriptionName { get; }
	}
}
