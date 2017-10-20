using System.Threading.Tasks;
using eventstore;
using shared;

namespace management
{
	public class ProvisionSubscriptionStream : IProvisionSubscriptionStreamRequests
	{
		private readonly ISubscriptionStreamProvisioner _subscriptionStreamProvisioner;

		public ProvisionSubscriptionStream(ISubscriptionStreamProvisioner subscriptionStreamProvisioner)
		{
			_subscriptionStreamProvisioner = subscriptionStreamProvisioner;
		}

		public Task Handle(IRecordedEvent<IProvisionSubscriptionStreamRequested> message)
		{
			return _subscriptionStreamProvisioner
				.RegisterSubscriptionStream<EventBusController>()
				.RegisterSubscriptionStream<ReconnectSubscriberWorkflow>()
				.ProvisionSubscriptionStream(message.Body.SubscriptionStream);
		}
	}
}

