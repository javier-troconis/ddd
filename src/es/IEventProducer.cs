using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace es
{
	public interface IEventProducer : IIdentity, IEventConsumer<IEvent>
	{
		int Version { get; }
		IReadOnlyCollection<IEvent> Changes { get; }
	}
}
