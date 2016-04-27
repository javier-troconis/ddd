using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace shared
{
    public static class EventDispatcher
    {
		public static TEventHandler Dispatch<TEventHandler>(TEventHandler handler, IEvent @event) where TEventHandler : IEventHandler
		{
			return Dispatch(handler, (dynamic)@event);
		}

		public static TEventHandler Dispatch<TEventHandler, TEvent>(TEventHandler handler, TEvent @event) where TEventHandler : IEventHandler where TEvent : IEvent
		{
			var specificHandler = handler as IEventHandler<TEvent, TEventHandler>;
			return specificHandler == null ? handler : specificHandler.Handle(@event);
		}
	}
}
