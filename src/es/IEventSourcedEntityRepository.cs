using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace es
{
    public interface IEventSourcedEntityRepository
    {
		void Load(Guid entityId, IEventConsumer entity);
		void Save(IEventProducer entity);
	}
}
