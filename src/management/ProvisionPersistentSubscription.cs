using System.Threading.Tasks;
using eventstore;
using shared;

namespace management
{
	public class ProvisionPersistentSubscription : IProvisionPersistentSubscriptionRequests
	{
		private readonly IPersistentSubscriptionProvisioner _persistentSubscriptionProvisioner;

		public ProvisionPersistentSubscription(IPersistentSubscriptionProvisioner persistentSubscriptionProvisioner)
		{
			_persistentSubscriptionProvisioner = persistentSubscriptionProvisioner;
		}

		public Task Handle(IRecordedEvent<IProvisionPersistentSubscriptionRequested> message)
		{
			return _persistentSubscriptionProvisioner
				.RegisterPersistentSubscription<ReconnectSubscriberWorkflow>()
				.RegisterPersistentSubscription<IProvisionSubscriptionStreamRequests, ProvisionSubscriptionStream>(
					x => x
                        .WithMaxRetriesOf(0)
                        .PreferDispatchToSingle()
                    )
				.ProvisionPersistentSubscription(message.Data.PersistentSubscriptionGroup);
		}
	}
}
