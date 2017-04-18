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

			Parallel.Invoke(
				() => _subscriptionStreamProvisioner.ProvisionPersistentSubscription<Subscriber3>(),
				() => _subscriptionStreamProvisioner.ProvisionPersistentSubscription<ISubscriptionStreamsProvisioningRequests, SubscriptionStreamsProvisioningRequestsHandler>()
				);
			return Task.CompletedTask;
		}
	}
}
