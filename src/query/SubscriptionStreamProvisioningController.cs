using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using eventstore;

using shared;

namespace query
{
	public class SubscriptionStreamProvisioningController : ISubscriptionStreamProvisioningController
	{
		private readonly ISubscriptionStreamProvisioningService _subscriptionStreamProvisioningService;

		public SubscriptionStreamProvisioningController(ISubscriptionStreamProvisioningService subscriptionStreamProvisioningService)
		{
			_subscriptionStreamProvisioningService = 
				subscriptionStreamProvisioningService
					.RegisterSubscriptionStream<Subscriber1>()
					.RegisterSubscriptionStream<Subscriber2>()
					.RegisterSubscriptionStream<Subscriber3>();
		}

		public Task Handle(IRecordedEvent<IProvisionSubscriptionStream> message)
		{
			return _subscriptionStreamProvisioningService.ProvisionSubscriptionStream(message.Data.SubscriptionStream);
		}

		public Task Handle(IRecordedEvent<IProvisionAllSubscriptionStreams> message)
		{
			return _subscriptionStreamProvisioningService.ProvisionAllSubscriptionStreams();
		}
	}
}

