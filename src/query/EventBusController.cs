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
	internal struct SubscriptionStarted : ISubscriptionStarted
	{
		public SubscriptionStarted(string subscriptionName)
		{
			SubscriptionName = subscriptionName;
		}

		public string SubscriptionName { get; }
	}

	internal struct SubscriptionStopped : ISubscriptionStopped
	{
		public SubscriptionStopped(string subscriptionName)
		{
			SubscriptionName = subscriptionName;
		}

		public string SubscriptionName { get; }
	}

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
			var subscriberStatuses = await _eventBus.StartSubscriber(message.Data.SubscriptionName);
			if (subscriberStatuses.Contains(new SubscriberStatus(message.Data.SubscriptionName, ConnectionStatus.Connected)))
			{
				await _eventPublisher.PublishEvent
					(
						new SubscriptionStarted(message.Data.SubscriptionName),
						x => message.Metadata.Aggregate(x, (y, z) => y.SetEventHeader(z.Key, z.Value)).SetCorrelationId(message.EventId)
					);
			}
		}

		public async Task Handle(IRecordedEvent<IStopSubscription> message)
		{
			var subscriberStatuses = await _eventBus.StopSubscriber(message.Data.SubscriptionName);
			if (subscriberStatuses.Contains(new SubscriberStatus(message.Data.SubscriptionName, ConnectionStatus.Disconnected)))
			{
				await _eventPublisher.PublishEvent
					(
						new SubscriptionStopped(message.Data.SubscriptionName),
						x => message.Metadata.Aggregate(x, (y, z) => y.SetEventHeader(z.Key, z.Value)).SetCorrelationId(message.EventId)
					);
			}
		}
	}
}
