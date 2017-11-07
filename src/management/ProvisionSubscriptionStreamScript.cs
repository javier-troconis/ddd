using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using eventstore;

namespace management
{
	public static class ProvisionSubscriptionStreamScript
	{
		public class Data
		{
			public string SubscriptionStreamName { get; set; }
			public string SubscriberName { get; set; }
		}

		public static readonly IReadOnlyList<Func<Data, object>> Activities =
			new Func<Data, object>[]
			{
				x => new StopSubscriber(x.SubscriberName),
				x => new StartSubscriber(x.SubscriberName),
				x => new ProvisionSubscriptionStream(x.SubscriptionStreamName),
				x => new StopSubscriber(x.SubscriberName),
				x => new ProvisionSubscriptionStream(x.SubscriptionStreamName),
				x => new StartSubscriber(x.SubscriberName),
				x => new StopSubscriber(x.SubscriberName),
				x => new StartSubscriber(x.SubscriberName)
			};
	}
}
