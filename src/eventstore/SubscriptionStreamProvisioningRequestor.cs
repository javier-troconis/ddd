using System;
using System.Threading.Tasks;

namespace eventstore
{
	public struct RequestSubscriptionStreamsProvisioningResult
	{
		public readonly Guid CorrelationId;

		internal RequestSubscriptionStreamsProvisioningResult(Guid correlationId)
		{
			CorrelationId = correlationId;
		}
	}

	public interface ISubscriptionStreamProvisioningRequestor
	{
		Task<RequestSubscriptionStreamsProvisioningResult> RequestSubscriptionStreamsProvisioning(string subscriptionStreamName);
	}

	
	public class SubscriptionStreamProvisioningRequestor : ISubscriptionStreamProvisioningRequestor
	{
		private readonly IEventPublisher _eventPublisher;

		public SubscriptionStreamProvisioningRequestor(IEventPublisher eventPublisher)
		{
			_eventPublisher = eventPublisher;
		}

		public async Task<RequestSubscriptionStreamsProvisioningResult> RequestSubscriptionStreamsProvisioning(string subscriptionStreamName)
		{
			var correlationId = Guid.NewGuid();
			await _eventPublisher.PublishEvent(new SubscriptionStreamsProvisioningRequested(subscriptionStreamName),
				configureEventDataSettings: x => x.SetCorrelationId(correlationId));
			return new RequestSubscriptionStreamsProvisioningResult(correlationId);
		}
	}
}
