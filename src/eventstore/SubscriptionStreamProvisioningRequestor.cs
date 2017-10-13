using System;
using System.Threading.Tasks;

namespace eventstore
{
	
	public interface ISubscriptionStreamProvisioningRequestor
	{
		Task RequestSubscriptionStreamsProvisioning(Guid correlationId, string subscriptionStreamName);
	}

	
	public class SubscriptionStreamProvisioningRequestor : ISubscriptionStreamProvisioningRequestor
	{
		private readonly IEventPublisher _eventPublisher;

		public SubscriptionStreamProvisioningRequestor(IEventPublisher eventPublisher)
		{
			_eventPublisher = eventPublisher;
		}

		public Task RequestSubscriptionStreamsProvisioning(Guid correlationId, string subscriptionStreamName)
		{
			return _eventPublisher.PublishEvent(
				new SubscriptionStreamsProvisioningRequested(subscriptionStreamName),
				x => x.SetCorrelationId(correlationId));
		}
	}
}
