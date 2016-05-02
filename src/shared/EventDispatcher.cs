using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace shared
{
    public static class EventDispatcher
    {
		public static TEventHandlerCandidate Dispatch<TEventHandlerCandidate>(TEventHandlerCandidate eventHandlerCandidate, IEvent @event)
		{
            return Dispatch(eventHandlerCandidate, (dynamic)@event);
		}

        private static TEventHandlerCandidate Dispatch<TEventHandlerCandidate, TEvent>(TEventHandlerCandidate eventHandlerCandidate, TEvent @event) where TEvent : IEvent
        {
            var eventHandler = eventHandlerCandidate as IEventHandler<TEvent, TEventHandlerCandidate>;
            return eventHandler == null ? eventHandlerCandidate : eventHandler.Handle(@event);
        }
    }
}
