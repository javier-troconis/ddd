using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace es
{
	public interface IEventConsumer
	{

	}

	public interface IEventConsumer<in TEvent> : IEventConsumer where TEvent : Event
	{
		void Apply(TEvent @event);
	}
}
