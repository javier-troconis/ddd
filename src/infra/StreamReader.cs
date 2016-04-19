using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using shared;

namespace infra
{
    public static class StreamReader<TState> where TState : IEventConsumer, new()
	{
		public static async Task<TState> GetStateAsync(Func<string, Task<IReadOnlyCollection<IEvent>>> streamReader, string fromStreamName)
		{
			var events = await streamReader(fromStreamName);
			return events.Aggregate(new TState(), (state, @event) => TryApplyEvent(state, (dynamic)@event));
		}

		private static TState TryApplyEvent<TEvent>(TState state, TEvent @event) where TEvent : IEvent
		{
			var eventConsumer = state as IEventConsumer<TEvent, TState>;
			return eventConsumer == null ? state : eventConsumer.Apply(@event);
		}
	}
}
