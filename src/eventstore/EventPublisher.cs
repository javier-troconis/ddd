using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using EventStore.ClientAPI;

namespace eventstore
{
	public interface IEventPublisher
	{
		Task PublishEvent(object @event, Func<EventDataSettings, EventDataSettings> configureEventDataSettings = null);
	}

	public class EventPublisher : IEventPublisher
	{
		private readonly IEventStore _eventStore;

		public EventPublisher(IEventStore eventStore)
		{
			_eventStore = eventStore;
		}

		public Task PublishEvent(object @event, Func<EventDataSettings, EventDataSettings> configureEventDataSettings)
		{
			var streamName = @event
				.GetType()
				.GetStreamName(Guid.NewGuid());
			return Task.WhenAll
			(
				_eventStore.WriteStreamMetadata(
					streamName,
					ExpectedVersion.NoStream,
					StreamMetadata.Create(
						maxAge: TimeSpan.FromSeconds(30))
						),
				_eventStore.WriteEvents(streamName, ExpectedVersion.NoStream, new[] { @event }, configureEventDataSettings)
			);
		}
	}
}
