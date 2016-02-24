using System;
using System.Threading.Tasks;
using shared;

namespace infra
{
    public class EventSourcedEntityRepository : IEventSourcedEntityRepository
    {
		private readonly Func<Guid, string> _toStreamName = entityId => entityId.ToString("N").ToLower();
		private readonly IEventStore _eventStore;

	    public EventSourcedEntityRepository(IEventStore eventStore)
		{
			_eventStore = eventStore;
		}

		public async Task Load(Guid entityId, IEventConsumer entity)
	    {
			var streamName = _toStreamName(entityId);
			var events = await _eventStore.GetEventsAsync(streamName);
			foreach (var @event in events)
			{
				@event.ApplyTo(entity);
			}
		}

	    public async Task Save(IEventProducer entity)
	    {
			var streamName = _toStreamName(entity.Id);
			var changes = entity.Events;
			var currentVersion = entity.Version;
			var expectedVersion = changes.Count > currentVersion ? -1 : currentVersion - changes.Count;
			await _eventStore.SaveEventsAsync(streamName, expectedVersion, changes);
	    }
    }
}
