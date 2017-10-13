using System;
using System.Threading.Tasks;

namespace eventstore
{
	public struct RequestPersistentSubscriptionsProvisioningResult
	{
		public readonly Guid CorrelationId;

		internal RequestPersistentSubscriptionsProvisioningResult(Guid correlationId)
		{
			CorrelationId = correlationId;
		}
	}

	public interface IPersistentSubscriptionProvisioningRequestor
	{
		Task<RequestPersistentSubscriptionsProvisioningResult> RequestPersistentSubscriptionsProvisioning(string persistentSubscriptionName);
	}

	public class PersistentSubscriptionProvisioningRequestor : IPersistentSubscriptionProvisioningRequestor
	{
		private readonly IEventPublisher _eventPublisher;

		public PersistentSubscriptionProvisioningRequestor(IEventPublisher eventPublisher)
		{
			_eventPublisher = eventPublisher;
		}

		public async Task<RequestPersistentSubscriptionsProvisioningResult> RequestPersistentSubscriptionsProvisioning(string persistentSubscriptionName)
		{
			var correlationId = Guid.NewGuid();
			await _eventPublisher.PublishEvent(
				new PersistentSubscriptionsProvisioningRequested(persistentSubscriptionName), 
				configureEventDataSettings: x => x.SetCorrelationId(correlationId));
			return new RequestPersistentSubscriptionsProvisioningResult(correlationId);
		}
	}

}
