﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using eventstore;

using shared;

namespace query
{
	public class PersistentSubscriptionProvisionerController : IProvisionPersistentSubscriptionRequests
	{
		private readonly IPersistentSubscriptionProvisioningService _persistentSubscriptionProvisioningService;

		public PersistentSubscriptionProvisionerController(IPersistentSubscriptionProvisioningService persistentSubscriptionProvisioningService)
		{
			_persistentSubscriptionProvisioningService = persistentSubscriptionProvisioningService;
		}

		public Task Handle(IRecordedEvent<IProvisionPersistentSubscriptionRequested> message)
		{
			return _persistentSubscriptionProvisioningService
				.RegisterPersistentSubscription<Subscriber3>()
				.RegisterPersistentSubscription<IProvisionSubscriptionStreamRequests, SubscriptionStreamProvisionerController>(
					x => x
                        .WithMaxRetriesOf(0)
                        .PreferDispatchToSingle()
                    )
				.ProvisionPersistentSubscription(message.Data.PersistentSubscriptionGroup);
		}
	}
}
