using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using eventstore;

using EventStore.ClientAPI.Common.Log;

using management.contracts;

using shared;

namespace subscriber
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
			if (!string.Equals(_serviceName, message.Data.ServiceName))
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
