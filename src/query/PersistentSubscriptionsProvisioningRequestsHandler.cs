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
		private readonly string _serviceName;
		private readonly IPersistentSubscriptionProvisioner _subscriptionStreamProvisioner;

		public PersistentSubscriptionsProvisioningRequestsHandler(string serviceName, IPersistentSubscriptionProvisioner subscriptionStreamProvisioner)
		{
			_serviceName = serviceName;
			_subscriptionStreamProvisioner = subscriptionStreamProvisioner;
		}


		public Task Handle(IRecordedEvent<IPersistentSubscriptionsProvisioningRequested> message)
		{
			return Task.WhenAll(
				_subscriptionStreamProvisioner.ProvisionPersistentSubscription<Subscriber3>(),
				_subscriptionStreamProvisioner.ProvisionPersistentSubscription<ISubscriptionStreamsProvisioningRequests, SubscriptionStreamsProvisioningRequestsHandler>()
				);
		}
	}
}
