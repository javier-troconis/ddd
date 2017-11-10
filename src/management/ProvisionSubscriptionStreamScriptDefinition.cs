using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using eventstore;

namespace management
{
	public class ProvisionSubscriptionStreamScriptData
	{
		public string SubscriptionStreamName { get; set; }
		public string SubscriberName { get; set; }
	}

	public class ProvisionSubscriptionStreamScriptDefinition : IScriptDefinition<ProvisionSubscriptionStreamScriptData>
	{
		public static readonly IScriptDefinition<ProvisionSubscriptionStreamScriptData> Instance = new ProvisionSubscriptionStreamScriptDefinition();

		private ProvisionSubscriptionStreamScriptDefinition()
		{
			
		}

		public string Type => nameof(ProvisionSubscriptionStreamScriptDefinition);

		public IReadOnlyList<Func<ProvisionSubscriptionStreamScriptData, object>> Activities =>
			new Func<ProvisionSubscriptionStreamScriptData, object>[]
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
