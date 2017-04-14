using System.Threading.Tasks;

namespace eventstore
{
	public interface IPersistentSubscriptionProvisioningRequestor
	{
		Task RequestPersistentSubscriptionsProvisioning(string serviceName, string persistentSubscriptionName);
	}

	public class PersistentSubscriptionProvisioningRequestor : IPersistentSubscriptionProvisioningRequestor
	{
		private readonly IEventPublisher _eventPublisher;

		public PersistentSubscriptionProvisioningRequestor(IEventPublisher eventPublisher)
		{
			_eventPublisher = eventPublisher;
		}

		public Task RequestPersistentSubscriptionsProvisioning(string serviceName, string persistentSubscriptionName)
		{
			return _eventPublisher.PublishEvent(new PersistentSubscriptionsProvisioningRequested(serviceName, persistentSubscriptionName));
		}
	}

}
