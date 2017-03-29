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
    public class RegisterSubscriptionProjectionHandler : IRegisterSubscriptionProjectionHandler
	{
		private readonly string _serviceName;

	    public RegisterSubscriptionProjectionHandler(string serviceName)
	    {
		    _serviceName = serviceName;
	    }

	    public Task Handle(IRecordedEvent<IRegisterSubscriptionProjection> message)
	    {
			if (!string.Equals(_serviceName, message.Data.ServiceName))
			{
				return Task.CompletedTask;
			}
			var projectionManager =
				new ProjectionManager(EventStoreSettings.ClusterDns, EventStoreSettings.ExternalHttpPort, EventStoreSettings.Username, EventStoreSettings.Password, new ConsoleLogger());
			return Task.WhenAll(
				ProjectionRegistry.RegisterSubscriptionProjection<Subscriber1>(projectionManager),
				ProjectionRegistry.RegisterSubscriptionProjection<Subscriber2>(projectionManager)
				);
		}
    }
}
