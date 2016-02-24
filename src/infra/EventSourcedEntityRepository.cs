using System;
using System.Threading.Tasks;
using shared;

namespace infra
{
    public class EventSourcedEntityRepository : IEventSourcedEntityRepository
    {
		private readonly Func<string, Guid, string> _toStreamName = (streamCategory, entityId) => $"{streamCategory}-{entityId.ToString("N").ToLower()}";
		private readonly IEventStore _eventStore;
	    private readonly string _streamCategory;

	    public EventSourcedEntityRepository(IEventStore eventStore, string streamCategory)
		{
			_eventStore = eventStore;
			_streamCategory = streamCategory;
		}

		public async Task Load(Guid entityId, IEventConsumer entity)
	    {
			var streamName = _toStreamName(_streamCategory, entityId);
			var events = await _eventStore.GetEventsAsync(streamName);
			foreach (var @event in events)
			{
				@event.ApplyTo(entity);
			}
		}

	    public async Task Save(IEventProducer entity)
	    {
			var streamName = _toStreamName(_streamCategory, entity.Id);
			var changes = entity.Events;
			var currentVersion = entity.Version;
			var expectedVersion = changes.Count > currentVersion ? -1 : currentVersion - changes.Count;
			await _eventStore.SaveEventsAsync(streamName, expectedVersion, changes);
	    }
    }
}
