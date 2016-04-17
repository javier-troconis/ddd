using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using shared;

namespace infra
{
    public static class StreamReader<TState> where TState : IEventConsumer, new()
	{
		public static async Task<TState> ReadAsync(Func<string, Task<IReadOnlyCollection<IEvent>>> streamReader, string fromStreamName)
		{
			var events = await streamReader(fromStreamName);
			return events.Aggregate(default(TState), (state, @event) => HandleEvent(state, (dynamic)@event));
		}

		private static TState HandleEvent<TEvent>(TState currentState, TEvent @event) where TEvent : IEvent
		{
			var nextState = currentState;
			if (Equals(default(TState), nextState))
			{
				nextState = new TState();
			}
			var eventConsumer = nextState as IEventConsumer<TEvent>;
			if (eventConsumer == null)
			{
				return currentState;
			}
			eventConsumer.Apply(@event);
			return nextState;
		}
	}
}
