using System.Threading.Tasks;
using eventstore;
using shared;

namespace management
{
	public class PersistentSubscriptionProvisioningController : IProvisionPersistentSubscriptionRequests
	{
		private readonly IPersistentSubscriptionProvisioningService _persistentSubscriptionProvisioningService;

		public PersistentSubscriptionProvisioningController(IPersistentSubscriptionProvisioningService persistentSubscriptionProvisioningService)
		{
			_persistentSubscriptionProvisioningService = persistentSubscriptionProvisioningService;
		}

		public Task Handle(IRecordedEvent<IProvisionPersistentSubscriptionRequested> message)
		{
			return _persistentSubscriptionProvisioningService
				.RegisterPersistentSubscription<RestartSubscriberWorkflow1Controller>()
				.RegisterPersistentSubscription<RestartSubscriberWorkflow2Controller>()
				.RegisterPersistentSubscription<IProvisionSubscriptionStreamRequests, SubscriptionStreamProvisioningController>(
					x => x
                        .WithMaxRetriesOf(0)
                        .PreferDispatchToSingle()
                    )
				.ProvisionPersistentSubscription(message.Data.PersistentSubscriptionGroup);
		}
	}
}
