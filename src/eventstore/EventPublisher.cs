using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using EventStore.ClientAPI;

namespace eventstore
{
	public interface IEventPublisher
	{
		Task PublishEvent(object @event, Func<EventConfiguration, EventConfiguration> configureEvent = null, string streamName = null, long? expectedVersion = null, TimeSpan? maxAge = null);
	}

	public class EventPublisher : IEventPublisher
	{
		private readonly IEventStore _eventStore;

		public EventPublisher(IEventStore eventStore)
		{
			_eventStore = eventStore;
		}

		public Task PublishEvent(object @event, Func<EventConfiguration, EventConfiguration> configureEvent, string streamName, long? expectedVersion, TimeSpan? maxAge)
		{
			streamName = streamName ?? @event
				.GetType()
				.GetStreamName(Guid.NewGuid());
			return Task.WhenAll
			(
				maxAge == null ? Task.CompletedTask :
					_eventStore.WriteStreamMetadata(
						streamName,
						ExpectedVersion.NoStream,
						StreamMetadata.Create(
							maxAge: maxAge)
							),
				_eventStore.WriteEvents(streamName, expectedVersion ?? ExpectedVersion.Any, new[] { @event }, configureEvent)
			);
		}
	}
}
