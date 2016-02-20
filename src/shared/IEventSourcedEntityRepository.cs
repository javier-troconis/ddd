using System;
using System.Threading.Tasks;

namespace shared
{
    public interface IEventSourcedEntityRepository
    {
		Task Load(Guid entityId, IEventConsumer entity);
		Task Save(IEventProducer entity);
	}
}
