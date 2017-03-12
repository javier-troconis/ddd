using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using shared;

namespace infra
{
    public static class StreamStateFolder
    {
        public static TState Fold<TState>(TState state, object @event)
        {
            return Fold(state, (dynamic)@event);
        }

        private static TState Fold<TState, TEvent>(TState state, TEvent @event)
        {
            var handler = state as IMessageHandler<TEvent, TState>;
            return Equals(handler, null) ? state : handler.Handle(@event);
        }
    }
}
