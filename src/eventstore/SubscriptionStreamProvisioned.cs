using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eventstore
{
	public interface ISubscriptionStreamProvisioned
	{
		string SubscriptionStreamName { get; }
	}


	public struct SubscriptionStreamProvisioned : ISubscriptionStreamProvisioned
    {
	    public string SubscriptionStreamName { get; }

	    public SubscriptionStreamProvisioned(string subscriptionStreamName)
	    {
		    SubscriptionStreamName = subscriptionStreamName;
	    }
    }
}
