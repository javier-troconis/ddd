using System;
using System.Threading.Tasks;
using shared;

namespace infra
{
    public static class StreamStateFactory
	{
		public static async Task<TState> Create<TState>(IEventStore eventStore, string streamName) where TState : IEventConsumer, new()
		{
			var state = new TState();
			var events = await eventStore.ReadEventsAsync(streamName);
			foreach (var @event in events)
			{
				@event.ApplyTo(state);
			}
			return state;
		}
    }
}
