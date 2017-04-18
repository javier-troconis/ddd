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
			Console.WriteLine("calling " + nameof(PersistentSubscriptionsProvisioningRequestsHandler) + " " + message.EventId);

			return _subscriptionStreamProvisioner
				.IncludePersistentSubscription<Subscriber3>()
				.IncludePersistentSubscription<ISubscriptionStreamsProvisioningRequests, SubscriptionStreamsProvisioningRequestsHandler>(
					x => x.PreferDispatchToSingle())
				.ProvisionPersistentSubscriptions(message.Event.PersistentSubscriptionGroup);
		}
	}
}
