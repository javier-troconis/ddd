using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eventstore
{
	public interface IPersistentSubscriptionProvisioned
	{
		string SubscriptionStreamName { get; }
		string PersistentSubscriptionGroupName { get; }
	}

	public struct PersistentSubscriptionProvisioned
    {
	    public string SubscriptionStreamName { get; }
	    public string PersistentSubscriptionGroupName { get; }

	    public PersistentSubscriptionProvisioned(string subscriptionStreamName, string persistentSubscriptionGroupName)
	    {
		    SubscriptionStreamName = subscriptionStreamName;
		    PersistentSubscriptionGroupName = persistentSubscriptionGroupName;
	    }
    }
}
