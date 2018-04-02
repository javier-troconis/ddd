using System.Threading.Tasks;
using eventstore;
using EventStore.ClientAPI.SystemData;
using shared;

namespace management
{
	public class SubscriptionStreamProvisionerController : IProvisionSubscriptionStreamRequests
	{
		private readonly ISubscriptionStreamProvisioner _subscriptionStreamProvisioner;

		public SubscriptionStreamProvisionerController(ISubscriptionStreamProvisioner subscriptionStreamProvisioner)
		{
			_subscriptionStreamProvisioner = subscriptionStreamProvisioner;
		}

		public Task Handle(IRecordedEvent<IProvisionSubscriptionStreamRequested> message)
		{
			return _subscriptionStreamProvisioner
				.RegisterSubscriptionStream<EventBusController>()
				//.RegisterSubscriptionStream<RestartSubscriberWorkflow1Controller>()
				//.RegisterSubscriptionStream<RestartSubscriberWorkflow2Controller>()
				.ProvisionSubscriptionStream(new UserCredentials(EventStoreSettings.Username, EventStoreSettings.Password), message.Data.SubscriptionStream);
		}
	}
}

