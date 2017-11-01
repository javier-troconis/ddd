using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using eventstore;

using shared;

namespace query
{
	public class PersistentSubscriptionProvisioningController : IPersistentSubscriptionProvisioningController
	{
		private readonly IEventPublisher _eventPublisher;
		private readonly IPersistentSubscriptionProvisioningService _persistentSubscriptionProvisioningService;

		public PersistentSubscriptionProvisioningController(IPersistentSubscriptionProvisioningService persistentSubscriptionProvisioningService, IEventPublisher eventPublisher)
		{
			_persistentSubscriptionProvisioningService = 
				persistentSubscriptionProvisioningService
					.RegisterPersistentSubscription<Subscriber3>()
					.RegisterPersistentSubscription<ISubscriptionStreamProvisioningController, SubscriptionStreamProvisioningController>(
						x => x
							.WithMaxRetriesOf(0)
							.PreferDispatchToSingle()
					);
			_eventPublisher = eventPublisher;
		}

		public async Task Handle(IRecordedEvent<IProvisionPersistentSubscription> message)
		{
			var status = await _persistentSubscriptionProvisioningService.ProvisionPersistentSubscription(message.Data.SubscriptionStream, message.Data.PersistentSubscriptionGroup);
			if (status == PersistentSubscriptionProvisioningStatus.Provisioned)
			{
				await _eventPublisher.PublishEvent
				(
					new object(),
					x => x.CopyMetadata(message.Metadata).SetCorrelationId(message.EventId)
				);
			}
		}

		public Task Handle(IRecordedEvent<IProvisionAllPersistentSubscriptions> message)
		{
			return _persistentSubscriptionProvisioningService.ProvisionAllPersistentSubscriptions();
		}
	}
}
