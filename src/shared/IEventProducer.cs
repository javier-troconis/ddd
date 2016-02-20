using System.Collections.Generic;

namespace shared
{
	public interface IEventProducer : IIdentity
	{
		int Version { get; }
		IReadOnlyCollection<Event> Events { get; }
	}
}
