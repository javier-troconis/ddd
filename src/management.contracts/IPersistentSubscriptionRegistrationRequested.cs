using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace management.contracts
{
	public interface IPersistentSubscriptionRegistrationRequested
	{
		string ServiceName { get; }
		string PersistentSubscriptionName { get; }
	}
}
