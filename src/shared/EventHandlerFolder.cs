using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace shared
{
    public static class EventHandlerFolder
    {
		public static TEventHandler Fold<TEventHandler>(TEventHandler eventHandler, IEvent @event) where TEventHandler : IMessageHandler
		{
            return TryFold(eventHandler, (dynamic)@event);
		}

        private static TEventHandler TryFold<TEventHandler, TEvent>(TEventHandler eventHandler, TEvent @event) where TEvent : IEvent
        {
            return CanFoldInto<TEventHandler, TEvent>(eventHandler) ? ((IMessageHandler<TEvent, TEventHandler>)eventHandler).Handle(@event) : eventHandler;
        }

        private static bool CanFoldInto<TEventHandler, TEvent>(TEventHandler eventHandler) where TEvent : IMessage
        {
            return eventHandler is IMessageHandler<TEvent, TEventHandler>;
        }
    }
}
