using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using command.contracts;
using eventstore;
using EventStore.ClientAPI;
using management.contracts;
using shared;

namespace query
{
    public class EventBusController :
	    IMessageHandler<IRecordedEvent<IStartSubscription>, Task>,
	    IMessageHandler<IRecordedEvent<IStopSubscription>, Task>

	{
		private readonly ConcurrentDictionary<string, Subscriber> _activeSubscribers = new ConcurrentDictionary<string, Subscriber>();
		private readonly Func<IEventStoreConnection> _createConnection;
		private readonly SubscriberRegistry _subscriberRegistry;
		
		public EventBusController(
			Func<IEventStoreConnection> createConnection, 
			SubscriberRegistry subscriberRegistry)
		{
			_createConnection = createConnection;
			_subscriberRegistry = subscriberRegistry;
		}

		public Task Handle(IRecordedEvent<IStartSubscription> message)
		{

			return Task.CompletedTask;
		}

		public Task Handle(IRecordedEvent<IStopSubscription> message)
		{
			return Task.CompletedTask;
		}
	}
}
