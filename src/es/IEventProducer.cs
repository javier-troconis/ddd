using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace es
{
	public interface IEventProducer : IIdentity, IEventConsumer<Event>
	{
		int Version { get; }
		IReadOnlyCollection<Event> Events { get; }
	}
}
