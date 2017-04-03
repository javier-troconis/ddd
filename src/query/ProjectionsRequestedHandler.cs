using System.Threading.Tasks;

using eventstore;

using management.contracts;

using shared;

namespace query
{
    public class ProjectionsRequestedHandler : IProjectionsRequestedHandler
	{
		private readonly string _serviceName;
	    private readonly ISubscriptionProjectionRegistry _subscriptionProjectionRegistry;

	    public ProjectionsRequestedHandler(string serviceName, ISubscriptionProjectionRegistry subscriptionProjectionRegistry)
	    {
		    _serviceName = serviceName;
		    _subscriptionProjectionRegistry = subscriptionProjectionRegistry;
	    }

	    public Task Handle(IRecordedEvent<IProjectionsRequested> message)
	    {
			if (!string.Equals(_serviceName, message.Event.ServiceName))
			{
				return Task.CompletedTask;
			}

			return Task.WhenAll(
				_subscriptionProjectionRegistry.CreateOrUpdateSubscriptionProjection<Subscriber1>(),
				_subscriptionProjectionRegistry.CreateOrUpdateSubscriptionProjection<Subscriber2>(),
				_subscriptionProjectionRegistry.CreateOrUpdateSubscriptionProjection<Subscriber3>()
				);
		}
    }
}
