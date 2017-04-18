using System.Threading.Tasks;

namespace eventstore
{
	public interface ISubscriptionStreamProvisioningRequestor
	{
		Task RequestSubscriptionStreamsProvisioning(string subscriptionStreamName);
	}

	
	public class SubscriptionStreamProvisioningRequestor : ISubscriptionStreamProvisioningRequestor
	{
		private readonly IEventPublisher _eventPublisher;

		public SubscriptionStreamProvisioningRequestor(IEventPublisher eventPublisher)
		{
			_eventPublisher = eventPublisher;
		}

		public Task RequestSubscriptionStreamsProvisioning(string subscriptionStreamName)
		{
			return _eventPublisher.PublishEvent(new SubscriptionStreamsProvisioningRequested(subscriptionStreamName));
		}
	}
}
