using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using eventstore;

using shared;

namespace query
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
				.RegisterPersistentSubscription<Subscriber3>()
				.RegisterPersistentSubscription<IProvisionSubscriptionStreamRequests, ProvisionSubscriptionStream>(
					x => x
                        .WithMaxRetriesOf(0)
                        .PreferDispatchToSingle()
                    )
				.ProvisionPersistentSubscription(message.Data.PersistentSubscriptionGroup);
		}
	}
}
