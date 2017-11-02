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
		private readonly IEventPublisher _eventPublisher;
		private readonly ISubscriptionStreamProvisioningService _subscriptionStreamProvisioningService;

		public SubscriptionStreamProvisioningController(ISubscriptionStreamProvisioningService subscriptionStreamProvisioningService, IEventPublisher eventPublisher)
		{ 
			_subscriptionStreamProvisioningService = 
				subscriptionStreamProvisioningService
					.RegisterSubscriptionStream<Subscriber1>()
					.RegisterSubscriptionStream<Subscriber2>()
					.RegisterSubscriptionStream<Subscriber3>();
			_eventPublisher = eventPublisher;
		}

		public async Task Handle(IRecordedEvent<IProvisionSubscriptionStream> message)
		{
			var status = await _subscriptionStreamProvisioningService.ProvisionSubscriptionStream(message.Data.SubscriptionStreamName);
			if (status == ProvisionSubscriptionStreamResult.Provisioned)
			{
				await _eventPublisher.PublishEvent
				(
					new SubscriptionStreamProvisioned(message.Data.SubscriptionStreamName),
					x => x.CopyMetadata(message.Metadata).SetCorrelationId(message.EventId)
				);
			}
		}

		public Task Handle(IRecordedEvent<IProvisionAllSubscriptionStreams> message)
		{
			return _subscriptionStreamProvisioningService.ProvisionAllSubscriptionStreams();
		}
	}
}

