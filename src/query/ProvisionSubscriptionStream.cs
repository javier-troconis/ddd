using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using eventstore;

using shared;

namespace query
{
	public class ProvisionSubscriptionStream : IProvisionSubscriptionStreamRequests
	{
		private readonly ISubscriptionStreamProvisioner _subscriptionStreamProvisioner;

		public ProvisionSubscriptionStream(ISubscriptionStreamProvisioner subscriptionStreamProvisioner)
		{
			_subscriptionStreamProvisioner = subscriptionStreamProvisioner;
		}

		public Task Handle(IRecordedEvent<IProvisionSubscriptionStreamRequested> message)
		{
			return _subscriptionStreamProvisioner
				.RegisterSubscriptionStream<EventBusController>()
				.RegisterSubscriptionStream<Subscriber1>()
				.RegisterSubscriptionStream<Subscriber2>()
				.RegisterSubscriptionStream<Subscriber3>()
				.ProvisionSubscriptionStream(message.Body.SubscriptionStream);
		}
	}
}

