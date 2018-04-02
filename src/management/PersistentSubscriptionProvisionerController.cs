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
				//.RegisterPersistentSubscription<RestartSubscriberWorkflow1Controller>()
				//.RegisterPersistentSubscription<RestartSubscriberWorkflow2Controller>()
				.RegisterPersistentSubscription<IProvisionSubscriptionStreamRequests, SubscriptionStreamProvisionerController>(
					x => x
                        .WithMaxRetriesOf(0)
                        .PreferDispatchToSingle()
                    )
				.ProvisionPersistentSubscription(message.Data.PersistentSubscriptionGroup);
		}
	}
}
