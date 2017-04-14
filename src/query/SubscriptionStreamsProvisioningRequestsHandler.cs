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
		private readonly string _serviceName;
		private readonly ISubscriptionStreamProvisioner _subscriptionStreamProvisioner;

		public SubscriptionStreamsProvisioningRequestsHandler(string serviceName, ISubscriptionStreamProvisioner subscriptionStreamProvisioner)
		{
			_serviceName = serviceName;
			_subscriptionStreamProvisioner = subscriptionStreamProvisioner;
		}

		public Task Handle(IRecordedEvent<ISubscriptionStreamsProvisioningRequested> message)
		{
			return Task.WhenAll(
				_subscriptionStreamProvisioner.ProvisionSubscriptionStream<Subscriber1>(),
				_subscriptionStreamProvisioner.ProvisionSubscriptionStream<Subscriber2>(),
				_subscriptionStreamProvisioner.ProvisionSubscriptionStream<Subscriber3>()
				);
		}
	}
}

