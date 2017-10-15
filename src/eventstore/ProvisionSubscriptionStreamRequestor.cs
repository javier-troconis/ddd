using System;
using System.Threading.Tasks;

namespace eventstore
{
	
	public interface IProvisionSubscriptionStreamRequestor
	{
		Task RequestSubscriptionStreamProvision(string subscriptionStreamName);
	}

	
	public class ProvisionProvisionSubscriptionStreamRequestor : IProvisionSubscriptionStreamRequestor
	{
		private readonly IEventPublisher _eventPublisher;

		public ProvisionProvisionSubscriptionStreamRequestor(IEventPublisher eventPublisher)
		{
			_eventPublisher = eventPublisher;
		}

		public Task RequestSubscriptionStreamProvision(string subscriptionStreamName)
		{
			return _eventPublisher.PublishEvent(
				new ProvisionSubscriptionStreamRequested(subscriptionStreamName));
		}
	}
}
