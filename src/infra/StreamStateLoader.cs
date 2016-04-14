using System;
using System.Threading.Tasks;
using shared;

namespace infra
{
    public static class StreamStateLoader
	{
		public static async Task<TState> Load<TState>(IEventStore eventStore, string streamName) where TState : IEventConsumer, new()
		{
			var state = new TState();
			var events = await eventStore.GetEventsAsync(streamName);
			foreach (var @event in events)
			{
				@event.ApplyTo(state);
			}
			return state;
		}
    }
}
