using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using shared;

namespace infra
{
    public static class StreamStateReader
	{
		public static async Task Read(Func<string, Task<IReadOnlyCollection<Event>>> streamReader, string fromStreamName, IEventConsumer toStreamState)
		{
			var events = await streamReader(fromStreamName);
			foreach (var @event in events)
			{
				@event.ApplyTo(toStreamState);
			}
		}
    }
}
