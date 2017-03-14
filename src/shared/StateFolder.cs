using System.Collections.Generic;
using System.Linq;

namespace shared
{
    public static class StateFolder
    {
	    public static TState FoldOver<TState>(this IEnumerable<object> events, TState state)
	    {
			return events.Aggregate(state, (x, y) => Handle(x, (dynamic)y));
		}

        private static TState Handle<TState, TEvent>(TState state, TEvent @event)
        {
            var handler = state as IMessageHandler<TEvent, TState>;
            return Equals(handler, null) ? state : handler.Handle(@event);
        }
    }
}
