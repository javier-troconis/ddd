using System.Collections.Generic;

namespace shared
{
	public interface IEventProducer : IIdentity
	{
		int Version { get; }
		IReadOnlyList<Event> Events { get; }
	}
}
