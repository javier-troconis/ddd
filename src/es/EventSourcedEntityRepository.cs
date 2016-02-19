using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace es
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
			var changes = entity.Changes;
			var currentVersion = entity.Version;
			var expectedVersion = changes.Count > currentVersion ? -1 : currentVersion - changes.Count; 
			EventStore.SaveEvents(entity.Id, expectedVersion, changes);
	    }
    }
}
