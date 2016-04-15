using System;
using System.Threading.Tasks;
using shared;

namespace infra
{
    public static class StreamStateReader
	{
		public static async Task Read(IEventStore eventStore, string fromStreamName, IEventConsumer toStreamState)
		{
			var events = await eventStore.ReadEventsAsync(fromStreamName);
			foreach (var @event in events)
			{
				@event.ApplyTo(toStreamState);
			}
		}
    }
}
