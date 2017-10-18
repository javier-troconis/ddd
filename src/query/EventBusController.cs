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
	    private readonly EventBus1 _eventBus;

		public EventBusController(EventBus1 eventBus)
		{
			_eventBus = eventBus;
		}

		public Task Handle(IRecordedEvent<IStartSubscription> message)
		{
			return _eventBus.StartSubscriber(message.Data.SubscriptionName);
		}

		public Task Handle(IRecordedEvent<IStopSubscription> message)
		{
			return _eventBus.StopSubscriber(message.Data.SubscriptionName);
		}
	}
}
