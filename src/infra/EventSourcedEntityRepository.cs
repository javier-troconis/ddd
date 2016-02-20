using System;
using System.Threading.Tasks;
using shared;

namespace infra
{
    public class EventSourcedEntityRepository : IEventSourcedEntityRepository
    {
		private readonly IEventStore _store;

		public EventSourcedEntityRepository(IEventStore store)
		{
			_store = store;
		}

		public async Task Load(Guid entityId, IEventConsumer entity)
	    {
			var events = await _store.GetEventsAsync(entityId.ToString(), int.MaxValue);
			foreach (var @event in events)
			{
				@event.ApplyTo(entity);
			}
		}

	    public async Task Save(IEventProducer entity)
	    {
			var changes = entity.Events;
			var currentVersion = entity.Version;
			var expectedVersion = changes.Count > currentVersion ? -1 : currentVersion - changes.Count;
			await _store.SaveEventsAsync(entity.Id.ToString(), expectedVersion, changes);
	    }
    }
}
