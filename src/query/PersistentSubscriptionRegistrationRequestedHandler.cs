using System.Threading.Tasks;

using eventstore;

using management.contracts;

using shared;

namespace query
{
	public class PersistentSubscriptionRegistrationRequestedHandler : IPersistentSubscriptionRegistrationRequestedHandler
	{
		private readonly string _serviceName;
		private readonly IPersistentSubscriptionRegistry _persistentSubscriptionRegistry;

		public PersistentSubscriptionRegistrationRequestedHandler(string serviceName, IPersistentSubscriptionRegistry persistentSubscriptionRegistry)
		{
			_serviceName = serviceName;
			_persistentSubscriptionRegistry = persistentSubscriptionRegistry;
		}

		public Task Handle(IRecordedEvent<IPersistentSubscriptionRegistrationRequested> message)
		{
			if (!string.Equals(_serviceName, message.Event.ServiceName))
			{
				return Task.CompletedTask;
			}

            return Task.WhenAll(
                _persistentSubscriptionRegistry.RegisterPersistentSubscription<ISubscriptionStreamRegistrationRequestedHandler, SubscriptionStreamRegistrationRequestedHandler>(),
                _persistentSubscriptionRegistry.RegisterPersistentSubscription<Subscriber3, Subscriber3>()
			);
		}

	}
}
