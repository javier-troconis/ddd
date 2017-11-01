using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using eventstore;

using shared;

namespace query
{
	public class SubscriptionStreamProvisionerController : IProvisionSubscriptionStreamRequests
	{
		private readonly ISubscriptionStreamProvisioningService _subscriptionStreamProvisioningService;

		public SubscriptionStreamProvisionerController(ISubscriptionStreamProvisioningService subscriptionStreamProvisioningService)
		{
			_subscriptionStreamProvisioningService = subscriptionStreamProvisioningService;
		}

		public Task Handle(IRecordedEvent<IProvisionSubscriptionStreamRequested> message)
		{
			return _subscriptionStreamProvisioningService
				.RegisterSubscriptionStream<Subscriber1>()
				.RegisterSubscriptionStream<Subscriber2>()
				.RegisterSubscriptionStream<Subscriber3>()
				.ProvisionSubscriptionStream(message.Data.SubscriptionStream);
		}
	}
}

