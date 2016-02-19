using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace es
{
	public interface IEventStream : IIdentity, IEventSourcedEntity<IEvent>
	{
		int Version { get; }
		IReadOnlyCollection<IEvent> Changes { get; }
	}
}
