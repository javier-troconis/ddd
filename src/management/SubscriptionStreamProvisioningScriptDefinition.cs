using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using eventstore;

namespace management
{
	public class SubscriptionStreamProvisioningScriptData
	{
		public string SubscriptionStreamName { get; set; }
		public string SubscriberName { get; set; }
	}

	public class SubscriptionStreamProvisioningScriptDefinition : IScriptDefinition<SubscriptionStreamProvisioningScriptData>
	{
		public static readonly IScriptDefinition<SubscriptionStreamProvisioningScriptData> Instance = new SubscriptionStreamProvisioningScriptDefinition();

		private SubscriptionStreamProvisioningScriptDefinition()
		{
			
		}

		public string Type => "SubscriptionStreamProvisioningScript";

		public IReadOnlyList<Func<SubscriptionStreamProvisioningScriptData, object>> Activities =>
			new Func<SubscriptionStreamProvisioningScriptData, object>[]
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
