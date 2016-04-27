using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace shared
{
    public static class EventHandlerDispatcher
    {
		public static TEventHandler Dispatch<TEventHandler>(TEventHandler subject, IEvent @event) where TEventHandler : IEventHandler
		{
			return Dispatch(subject, (dynamic)@event);
		}

		private static TEventHandler Dispatch<TEventHandler, TEvent>(TEventHandler subject, TEvent @event) where TEventHandler : IEventHandler where TEvent : IEvent
		{
			var eventHandler = subject as IEventHandler<TEvent, TEventHandler>;
			return eventHandler == null ? subject : eventHandler.Handle(@event);
		}
	}
}
