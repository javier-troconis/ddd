using System.Threading.Tasks;
using eventstore;
using shared;

namespace management
{
	public class SubscriptionStreamProvisioningController : IProvisionSubscriptionStreamRequests
	{
		private readonly ISubscriptionStreamProvisioningService _subscriptionStreamProvisioningService;

		public SubscriptionStreamProvisioningController(ISubscriptionStreamProvisioningService subscriptionStreamProvisioningService)
		{
			_subscriptionStreamProvisioningService = subscriptionStreamProvisioningService;
		}

		public Task Handle(IRecordedEvent<IProvisionSubscriptionStreamRequested> message)
		{
			return _subscriptionStreamProvisioningService
				.RegisterSubscriptionStream<EventBusController>()
				.RegisterSubscriptionStream<RestartSubscriberWorkflow1Controller>()
				.RegisterSubscriptionStream<RestartSubscriberWorkflow2Controller>()
				.ProvisionSubscriptionStream(message.Data.SubscriptionStream);
		}
	}
}

