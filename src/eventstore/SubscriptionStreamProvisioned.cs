using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eventstore
{
	public interface ISubscriptionStreamProvisioned
	{
		string SubscriptionStream { get; }
	}


	public struct SubscriptionStreamProvisioned
    {
	    public string SubscriptionStream { get; }

	    public SubscriptionStreamProvisioned(string subscriptionStream)
	    {
		    SubscriptionStream = subscriptionStream;
	    }
    }
}
