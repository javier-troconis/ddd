using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using shared;

namespace infra
{
    public static class StreamStateFolder
    {
        public static TState Fold<TState>(TState state, IEvent @event) where TState : IMessageHandler
        {
            return Fold(state, (dynamic)@event);
        }

        private static TState Fold<TState, TEvent>(TState state, TEvent @event) where TState : IMessageHandler
        {
            var handler = MessageHandlerDelegate.TryCreateFrom<TEvent, TState>(state);
            return Equals(handler, null) ? state : handler.Handle(@event);
        }
    }
}
