using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace es
{
	public interface IEventProducer : IIdentity
	{
		int Version { get; }
		IReadOnlyCollection<Event> Events { get; }
	}
}
