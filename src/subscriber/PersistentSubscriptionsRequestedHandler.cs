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
	public class PersistentSubscriptionsRequestedHandler : IPersistentSubscriptionsRequestedHandler
	{
		private readonly string _serviceName;
		private readonly IPersistentSubscriptionRegistry _persistentSubscriptionRegistry;

		public PersistentSubscriptionsRequestedHandler(string serviceName, IPersistentSubscriptionRegistry persistentSubscriptionRegistry)
		{
			_serviceName = serviceName;
			_persistentSubscriptionRegistry = persistentSubscriptionRegistry;
		}

		public Task Handle(IRecordedEvent<IPersistentSubscriptionsRequested> message)
		{
			if (!string.Equals(_serviceName, message.Data.ServiceName))
			{
				return Task.CompletedTask;
			}

			return Task.WhenAll(
				_persistentSubscriptionRegistry.RegisterPersistentSubscription<IProjectionsRequestedHandler, ProjectionsRequestedHandler>(),
				_persistentSubscriptionRegistry.RegisterPersistentSubscription<Subscriber3, Subscriber3>()
			);
		}

	}
}
