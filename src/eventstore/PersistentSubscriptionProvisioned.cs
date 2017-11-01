using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eventstore
{
	public interface IPersistentSubscriptionProvisioned
	{
		string SubscriptionStream { get; }
		string PersistentSubscriptionGroup { get; }
	}

	public struct PersistentSubscriptionProvisioned
    {
	    public string SubscriptionStream { get; }
	    public string PersistentSubscriptionGroup { get; }

	    public PersistentSubscriptionProvisioned(string subscriptionStream, string persistentSubscriptionGroup)
	    {
		    SubscriptionStream = subscriptionStream;
		    PersistentSubscriptionGroup = persistentSubscriptionGroup;
	    }
    }
}
