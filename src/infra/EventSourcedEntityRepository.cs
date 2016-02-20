using System;
using ConsoleApp3;
using shared;

namespace infra
{
    public class EventSourcedEntityRepository : IEventSourcedEntityRepository
    {
	    public void Load(Guid entityId, IEventConsumer entity)
	    {
			var events = EventStore.GetEvents(entityId);
			foreach (var @event in events)
			{
				@event.ApplyTo(entity);
			}
		}

	    public void Save(IEventProducer entity)
	    {
			var changes = entity.Events;
			var currentVersion = entity.Version;
			var expectedVersion = changes.Count > currentVersion ? -1 : currentVersion - changes.Count; 
			EventStore.SaveEvents(entity.Id, expectedVersion, changes);
	    }
    }
}
