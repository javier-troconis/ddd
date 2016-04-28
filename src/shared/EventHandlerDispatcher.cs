using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace shared
{
    public static class EventDispatcher
    {
		public static TResult Dispatch<TResult>(TResult eventHandler, IEvent @event) where TResult : IEventHandler
		{
			return Dispatch(eventHandler, (dynamic)@event);
		}

		public static TResult Dispatch<TResult, TEvent>(TResult eventHandler, TEvent @event) where TResult : IEventHandler where TEvent : IEvent
		{
			var specificEventHandler = eventHandler as IEventHandler<TEvent, TResult>;
			return specificEventHandler == null ? eventHandler : specificEventHandler.Handle(@event);
		}
	}
}
