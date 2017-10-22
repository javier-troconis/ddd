using System.Linq;
using System.Threading.Tasks;
using eventstore;
using management.contracts;
using shared;

namespace management
{
	internal struct SubscriberStarted : ISubscriberStarted
	{
		public SubscriberStarted(string subscriberName)
		{
			SubscriberName = subscriberName;
		}

		public string SubscriberName { get; }
	}

	internal struct SubscriberStopped : ISubscriberStopped
	{
		public SubscriberStopped(string subscriberName)
		{
			SubscriberName = subscriberName;
		}

		public string SubscriberName { get; }
	}

	public class EventBusController :
	    IMessageHandler<IRecordedEvent<IStartSubscriber>, Task>,
	    IMessageHandler<IRecordedEvent<IStopSubscriber>, Task>
    {
	    private readonly EventBus _eventBus;
	    private readonly IEventPublisher _eventPublisher;

	    public EventBusController(EventBus eventBus, IEventPublisher eventPublisher)
	    {
		    _eventBus = eventBus;
		    _eventPublisher = eventPublisher;
	    }

		public async Task Handle(IRecordedEvent<IStartSubscriber> message)
		{
			var status = await _eventBus.StartSubscriber(message.Body.SubscriberName);
			if (status == SubscriberStatus.Connected)
			{
				await _eventPublisher.PublishEvent
					(
						new SubscriberStarted(message.Body.SubscriberName),
						x => message.Header.Aggregate(x, (y, z) => y.SetEntry(z.Key, z.Value)).SetCorrelationId(message.Header.EventId)
					);
			}
		}

		public async Task Handle(IRecordedEvent<IStopSubscriber> message)
		{
			var status = await _eventBus.StopSubscriber(message.Body.SubscriberName);
			if (status == SubscriberStatus.NotConnected)
			{
				await _eventPublisher.PublishEvent
					(
						new SubscriberStopped(message.Body.SubscriberName),
						x => message.Header.Aggregate(x, (y, z) => y.SetEntry(z.Key, z.Value)).SetCorrelationId(message.Header.EventId)
					);
			}
		}
	}
}
