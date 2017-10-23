using System.Threading.Tasks;
using eventstore;
using shared;

namespace management
{
	public class PersistentSubscriptionProvisionerController : IProvisionPersistentSubscriptionRequests
	{
		private readonly IPersistentSubscriptionProvisioner _persistentSubscriptionProvisioner;

		public PersistentSubscriptionProvisionerController(IPersistentSubscriptionProvisioner persistentSubscriptionProvisioner)
		{
			_persistentSubscriptionProvisioner = persistentSubscriptionProvisioner;
		}

		public Task Handle(IRecordedEvent<IProvisionPersistentSubscriptionRequested> message)
		{
			return _persistentSubscriptionProvisioner
				.RegisterPersistentSubscription<ReconnectSubscriberWorkflow>()
				.RegisterPersistentSubscription<IProvisionSubscriptionStreamRequests, SubscriptionStreamProvisionerController>(
					x => x
                        .WithMaxRetriesOf(0)
                        .PreferDispatchToSingle()
                    )
				.ProvisionPersistentSubscription(message.Data.PersistentSubscriptionGroup);
		}
	}
}
