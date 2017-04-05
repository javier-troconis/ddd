using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace management.contracts
{
    public interface ISubscriptionStreamRegistrationRequested
    {
		string ServiceName { get; }
		string SubscriptionStreamName { get; }
	}
}
