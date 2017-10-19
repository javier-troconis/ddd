using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using management.contracts;

namespace query
{
	internal struct SubscriptionStarted : ISubscriptionStarted
	{
		public SubscriptionStarted(string subscriptionName)
		{
			SubscriptionName = subscriptionName;
		}

		public string SubscriptionName { get; }
	}

	internal struct SubscriptionStopped : ISubscriptionStopped
	{
		public SubscriptionStopped(string subscriptionName)
		{
			SubscriptionName = subscriptionName;
		}

		public string SubscriptionName { get; }
	}
}
