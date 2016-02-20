using System;

namespace shared
{
    public interface IEventSourcedEntityRepository
    {
		void Load(Guid entityId, IEventConsumer entity);
		void Save(IEventProducer entity);
	}
}
