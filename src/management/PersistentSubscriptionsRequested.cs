using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using management.contracts;

namespace management
{
    public struct PersistentSubscriptionsRequested : IPersistentSubscriptionsRequested
	{
		public PersistentSubscriptionsRequested(string serviceName, string groupName)
		{
			ServiceName = serviceName;
			GroupName = groupName;
		}

		public string ServiceName { get; }
		public string GroupName { get; }
	}
}
