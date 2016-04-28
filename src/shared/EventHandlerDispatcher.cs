using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace shared
{
    public static class EventDispatcher
    {
		public static TEventHandler Dispatch<TEventHandler>(TEventHandler eventHandler, IEvent @event) 
            where TEventHandler : IEventHandler
		{
			return Dispatch(eventHandler, (dynamic)@event);
		}

		public static TEventHandler Dispatch<TEventHandler, TEvent>(TEventHandler eventHandler, TEvent @event) 
            where TEventHandler : IEventHandler<TEvent, TEventHandler> where TEvent : IEvent
		{
			var specificEventHandler = eventHandler as IEventHandler<TEvent, TEventHandler>;
			return specificEventHandler == null ? eventHandler : specificEventHandler.Handle(@event);
		}
	}
}
