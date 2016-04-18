using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using shared;

namespace infra
{
    public static class StreamState<TState> where TState : IEventConsumer, new()
	{
		public static async Task<TState> GetAsync(Func<string, Task<IReadOnlyCollection<IEvent>>> streamReader, string fromStreamName)
		{
			var events = await streamReader(fromStreamName);
			return events.Aggregate(new TState(), (state, @event) => ApplyEvent(state, (dynamic)@event));
		}

		private static TState ApplyEvent<TEvent>(TState state, TEvent @event) where TEvent : IEvent
		{
			var handler = state as IEventConsumer<TEvent, TState>;
			return handler == null ? state : handler.When(@event);
		}
	}
}
