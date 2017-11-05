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
				subscriptionStreamProvisioningService
					.RegisterSubscriptionStream<EventBusController>()
					.RegisterSubscriptionStream<ProvisionSubscriptionStreamScriptController>();
		}

		public Task Handle(IRecordedEvent<IProvisionSubscriptionStream> message)
		{
			return _subscriptionStreamProvisioningService.ProvisionSubscriptionStream(message.Data.SubscriptionStreamName);
		}

		public Task Handle(IRecordedEvent<IProvisionAllSubscriptionStreams> message)
		{
			return _subscriptionStreamProvisioningService.ProvisionAllSubscriptionStreams();
		}
	}
}

