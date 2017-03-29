using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace management.contracts
{
	public interface IPersistentSubscriptionsRequested
	{
		string ServiceName { get; }
		string GroupName { get; }
	}
}
