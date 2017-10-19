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
	    private readonly EventBus _eventBus;
	    private readonly IEventPublisher _eventPublisher;

	    public EventBusController(EventBus eventBus, IEventPublisher eventPublisher)
	    {
		    _eventBus = eventBus;
		    _eventPublisher = eventPublisher;
	    }

		public async Task Handle(IRecordedEvent<IStartSubscription> message)
		{
			var subscriberName = message.Data.SubscriptionName;
			var subscriberStatuses = await _eventBus.StartSubscriber(subscriberName);
			if (subscriberStatuses.Contains(new SubscriberStatus(subscriberName, ConnectionStatus.Connected)))
			{
				await _eventPublisher.PublishEvent(new SubscriptionStarted(subscriberName));
			}
		}

		public async Task Handle(IRecordedEvent<IStopSubscription> message)
		{
			var subscriberName = message.Data.SubscriptionName;
			var subscriberStatuses = await _eventBus.StopSubscriber(subscriberName);
			if (subscriberStatuses.Contains(new SubscriberStatus(subscriberName, ConnectionStatus.Disconnected)))
			{
				await _eventPublisher.PublishEvent(new SubscriptionStopped(subscriberName));
			}
		}
	}
}
