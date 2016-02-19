using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace es
{
	public interface IEventSourcedEntity
	{

	}

	public interface IEventSourcedEntity<in TEvent> : IEventSourcedEntity where TEvent : IEvent
	{
		void Apply(TEvent @event);
	}
}
