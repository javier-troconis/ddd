using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using shared;

namespace infra
{
    public class EventSourcedEntityRepositoryFactory : IEventSourcedEntityRepositoryFactory
    {
		private readonly IEventStore _eventStore;

		public EventSourcedEntityRepositoryFactory(IEventStore eventStore)
		{
			_eventStore = eventStore;
		}

		public IEventSourcedEntityRepository CreateForStreamCategory(string streamCategory)
	    {
			return new EventSourcedEntityRepository(_eventStore, streamCategory);
	    }
    }
}
