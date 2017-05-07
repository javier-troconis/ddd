using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using eventstore;

using shared;

namespace query
{
	public class PersistentSubscriptionsProvisioningRequestsHandler : IPersistentSubscriptionsProvisioningRequests
	{
		private readonly IPersistentSubscriptionProvisioner _subscriptionStreamProvisioner;

		public PersistentSubscriptionsProvisioningRequestsHandler(IPersistentSubscriptionProvisioner subscriptionStreamProvisioner)
		{
			_subscriptionStreamProvisioner = subscriptionStreamProvisioner;
		}

		public Task Handle(IRecordedEvent<IPersistentSubscriptionsProvisioningRequested> message)
		{
			return _subscriptionStreamProvisioner
				.RegisterPersistentSubscriptionProvisioning<Subscriber3>()
				.RegisterPersistentSubscriptionProvisioning<ISubscriptionStreamsProvisioningRequests, SubscriptionStreamsProvisioningRequestsHandler>(
					x => x.WithMaxRetriesOf(0))
				.ProvisionPersistentSubscriptions(message.Event.PersistentSubscriptionGroup);
		}
	}
}
