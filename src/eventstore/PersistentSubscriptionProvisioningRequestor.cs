using System;
using System.Threading.Tasks;

namespace eventstore
{
	public interface IPersistentSubscriptionProvisioningRequestor
	{
		Task RequestPersistentSubscriptionsProvisioning(Guid correlationId, string persistentSubscriptionName);
	}

	public class PersistentSubscriptionProvisioningRequestor : IPersistentSubscriptionProvisioningRequestor
	{
		private readonly IEventPublisher _eventPublisher;

		public PersistentSubscriptionProvisioningRequestor(IEventPublisher eventPublisher)
		{
			_eventPublisher = eventPublisher;
		}

		public Task RequestPersistentSubscriptionsProvisioning(Guid correlationId, string persistentSubscriptionName)
		{
			return _eventPublisher.PublishEvent(
				new PersistentSubscriptionsProvisioningRequested(persistentSubscriptionName), 
				x => x.SetCorrelationId(correlationId));
		}
	}

}
