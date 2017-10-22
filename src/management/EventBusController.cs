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
			var connectionStatus = await _eventBus.StartSubscriber(message.Body.SubscriberName);
			if (connectionStatus == ConnectionStatus.Connected)
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
			var connectionStatus = await _eventBus.StopSubscriber(message.Body.SubscriberName);
			if (connectionStatus == ConnectionStatus.NotConnected)
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
