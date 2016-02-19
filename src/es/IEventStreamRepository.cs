using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace es
{
    public interface IEventStreamRepository
    {
		void Load(Guid entityId, IEventSourcedEntity entity);
		void Save(IEventStream entity);
	}
}
