using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using management.contracts;

namespace management
{
    public struct PersistentSubscriptionRegistrationRequested : IPersistentSubscriptionRegistrationRequested
	{
		public PersistentSubscriptionRegistrationRequested(string serviceName, string persistentSubscriptionName)
		{
			ServiceName = serviceName;
			PersistentSubscriptionName = persistentSubscriptionName;
		}

		public string ServiceName { get; }
		public string PersistentSubscriptionName { get; }
	}
}
