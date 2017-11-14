using System.Threading.Tasks;
using eventstore;
using shared;

namespace management
{
	public class PersistentSubscriptionProvisioningController : IPersistentSubscriptionProvisioningController
	{
		private readonly IPersistentSubscriptionProvisioningService _persistentSubscriptionProvisioningService;

		public PersistentSubscriptionProvisioningController(IPersistentSubscriptionProvisioningService persistentSubscriptionProvisioningService)
		{
			_persistentSubscriptionProvisioningService = 
				persistentSubscriptionProvisioningService
					.RegisterPersistentSubscription<SubscriptionStreamProvisioningScriptController>()
					.RegisterPersistentSubscription<ISubscriptionStreamProvisioningController, SubscriptionStreamProvisioningController>(
						x => x
							.WithMaxRetriesOf(0)
							.PreferDispatchToSingle()
					);
		}

		public Task Handle(IRecordedEvent<IProvisionPersistentSubscription> message)
		{
			return _persistentSubscriptionProvisioningService.ProvisionPersistentSubscription(message.Data.SubscriptionStreamName, message.Data.PersistentSubscriptionGroupName);
		}

		public Task Handle(IRecordedEvent<IProvisionAllPersistentSubscriptions> message)
		{
			return _persistentSubscriptionProvisioningService.ProvisionAllPersistentSubscriptions();
		}
	}
}
