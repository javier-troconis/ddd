using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using eventstore;

using shared;

namespace query
{
	public class SubscriptionStreamsProvisioningRequestsHandler : ISubscriptionStreamsProvisioningRequests
	{
		private readonly ISubscriptionStreamProvisioner _subscriptionStreamProvisioner;

		public SubscriptionStreamsProvisioningRequestsHandler(ISubscriptionStreamProvisioner subscriptionStreamProvisioner)
		{
			_subscriptionStreamProvisioner = subscriptionStreamProvisioner;
		}

		public Task Handle(IRecordedEvent<ISubscriptionStreamsProvisioningRequested> message)
		{
			Console.WriteLine("calling " + nameof(SubscriptionStreamsProvisioningRequestsHandler) + " " + message.EventId);
			// static queue per subscriptionstream provisioning
			Parallel.Invoke(
				() => _subscriptionStreamProvisioner.ProvisionSubscriptionStream<Subscriber1>(),
				() => _subscriptionStreamProvisioner.ProvisionSubscriptionStream<Subscriber2>(),
				() => _subscriptionStreamProvisioner.ProvisionSubscriptionStream<Subscriber3>()
				);
			return Task.CompletedTask;
		}
	}
}

