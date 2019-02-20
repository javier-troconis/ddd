using System.Threading.Tasks;
using shared;

namespace eventstore
{
	[Topic]
	public interface IStopSubscriber
	{
		string SubscriberName { get; }
	}

	[Topic]
	public interface ISubscriberStopped
	{
		string SubscriberName { get; }
	}

	[Topic]
	public interface IStartSubscriber
	{
		string SubscriberName { get; }
	}

	[Topic]
	public interface ISubscriberStarted
	{
		string SubscriberName { get; }
	}

	public struct StartSubscriber : IStartSubscriber
	{
		public StartSubscriber(string subscriberName)
		{
			SubscriberName = subscriberName;
		}

		public string SubscriberName { get; }
	}

	public struct SubscriberStarted : ISubscriberStarted
	{
		public SubscriberStarted(string subscriberName)
		{
			SubscriberName = subscriberName;
		}

		public string SubscriberName { get; }
	}

	public struct StopSubscriber : IStopSubscriber
	{
		public StopSubscriber(string subscriberName)
		{
			SubscriberName = subscriberName;
		}

		public string SubscriberName { get; }
	}

	public struct SubscriberStopped : ISubscriberStopped
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
		private readonly IEventBus _eventBus;
		private readonly IEventPublisher _eventPublisher;

		public EventBusController(IEventBus eventBus, IEventPublisher eventPublisher)
		{
			_eventBus = eventBus;
			_eventPublisher = eventPublisher;
		}

		public async Task Handle(IRecordedEvent<IStartSubscriber> message)
		{
			var status = await _eventBus.StartSubscriber(message.Data.SubscriberName);
			if (status == StartSubscriberResult.Started)
			{
				await _eventPublisher.PublishEvent
				(
					new SubscriberStarted(message.Data.SubscriberName),
					x => x.CopyMetadata(message.Metadata).SetCorrelationId(message.EventId)
				);
			}
		}

		public async Task Handle(IRecordedEvent<IStopSubscriber> message)
		{
			var status = await _eventBus.StopSubscriber(message.Data.SubscriberName);
			if (status == StopSubscriberResult.Stopped)
			{
				await _eventPublisher.PublishEvent
				(
					new SubscriberStopped(message.Data.SubscriberName),
					x => x.CopyMetadata(message.Metadata).SetCorrelationId(message.EventId)
				);
			}
		}
	}
}