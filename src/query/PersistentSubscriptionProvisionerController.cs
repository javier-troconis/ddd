using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using eventstore;

using shared;

namespace query
{
	public class PersistentSubscriptionProvisionerController : IPersistentSubscriptionProvisioningController
	{
		private readonly IPersistentSubscriptionProvisioningService _persistentSubscriptionProvisioningService;

		public PersistentSubscriptionProvisionerController(IPersistentSubscriptionProvisioningService persistentSubscriptionProvisioningService)
		{
			_persistentSubscriptionProvisioningService = 
				persistentSubscriptionProvisioningService
					.RegisterPersistentSubscription<Subscriber3>()
					.RegisterPersistentSubscription<ISubscriptionStreamProvisioningController, SubscriptionStreamProvisionerController>(
						x => x
							.WithMaxRetriesOf(0)
							.PreferDispatchToSingle()
					);
		}

		public Task Handle(IRecordedEvent<IProvisionPersistentSubscription> message)
		{
			return _persistentSubscriptionProvisioningService
				.ProvisionPersistentSubscription(message.Data.PersistentSubscriptionGroup);
		}

		public Task Handle(IRecordedEvent<IProvisionAllPersistentSubscriptions> message)
		{
			return _persistentSubscriptionProvisioningService.ProvisionAllPersistentSubscriptions();
		}
	}
}
