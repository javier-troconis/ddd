using System.Threading.Tasks;

using eventstore;

using management.contracts;

using shared;

namespace query
{
    public class SubscriptionStreamRegistrationRequestedHandler : ISubscriptionStreamRegistrationRequestedHandler
	{
		private readonly string _serviceName;
	    private readonly ISubscriptionProjectionRegistry _subscriptionProjectionRegistry;

	    public SubscriptionStreamRegistrationRequestedHandler(string serviceName, ISubscriptionProjectionRegistry subscriptionProjectionRegistry)
	    {
		    _serviceName = serviceName;
		    _subscriptionProjectionRegistry = subscriptionProjectionRegistry;
	    }

	    public Task Handle(IRecordedEvent<ISubscriptionStreamRegistrationRequested> message)
	    {
			if (!string.Equals(_serviceName, message.Event.ServiceName))
			{
				return Task.CompletedTask;
			}

			return Task.WhenAll(
				_subscriptionProjectionRegistry.RegisterSubscriptionProjection<Subscriber1>(),
				_subscriptionProjectionRegistry.RegisterSubscriptionProjection<Subscriber2>(),
				_subscriptionProjectionRegistry.RegisterSubscriptionProjection<Subscriber3>()
				);
		}
    }
}
