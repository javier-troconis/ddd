using System;
using System.Threading.Tasks;

namespace eventstore
{
	
	public interface IProvisionSubscriptionStreamRequestor
	{
		Task RequestSubscriptionStreamProvision(Guid correlationId, string subscriptionStreamName);
	}

	
	public class ProvisionProvisionSubscriptionStreamRequestor : IProvisionSubscriptionStreamRequestor
	{
		private readonly IEventPublisher _eventPublisher;

		public ProvisionProvisionSubscriptionStreamRequestor(IEventPublisher eventPublisher)
		{
			_eventPublisher = eventPublisher;
		}

		public Task RequestSubscriptionStreamProvision(Guid correlationId, string subscriptionStreamName)
		{
			return _eventPublisher.PublishEvent(
				new ProvisionSubscriptionStreamRequested(subscriptionStreamName),
				x => x.SetCorrelationId(correlationId));
		}
	}
}
