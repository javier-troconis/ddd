﻿using System;
using System.Threading.Tasks;

namespace eventstore
{
	public interface IProvisionPersistentSubscriptionRequestor
	{
		Task RequestPersistentSubscriptionProvision(string persistentSubscriptionName);
	}

	public class ProvisionPersistentSubscriptionRequestor : IProvisionPersistentSubscriptionRequestor
	{
		private readonly IEventPublisher _eventPublisher;

		public ProvisionPersistentSubscriptionRequestor(IEventPublisher eventPublisher)
		{
			_eventPublisher = eventPublisher;
		}

		public Task RequestPersistentSubscriptionProvision(string persistentSubscriptionName)
		{
			return _eventPublisher.PublishEvent(
				new ProvisionPersistentSubscriptionRequested(persistentSubscriptionName));
		}
	}

}