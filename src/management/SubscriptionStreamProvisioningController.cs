using System.Threading.Tasks;
using eventstore;
using shared;

namespace management
{
	public class SubscriptionStreamProvisioningController : ISubscriptionStreamProvisioningController
	{
		private readonly ISubscriptionStreamProvisioningService _subscriptionStreamProvisioningService;

		public SubscriptionStreamProvisioningController(ISubscriptionStreamProvisioningService subscriptionStreamProvisioningService)
		{
			_subscriptionStreamProvisioningService =
				subscriptionStreamProvisioningService.RegisterSubscriptionStream<EventBusController>()
					.RegisterSubscriptionStream<RestartSubscriberWorkflow1Controller>()
					.RegisterSubscriptionStream<RestartSubscriberWorkflow2Controller>();
		}

		public Task Handle(IRecordedEvent<IProvisionSubscriptionStream> message)
		{
			return _subscriptionStreamProvisioningService.ProvisionSubscriptionStream(message.Data.SubscriptionStream);
		}

		public Task Handle(IRecordedEvent<IProvisionAllSubscriptionStreams> message)
		{
			return _subscriptionStreamProvisioningService.ProvisionAllSubscriptionStreams();
		}
	}
}

