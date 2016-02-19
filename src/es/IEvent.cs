using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace es
{
    public interface IEvent
    {
		Guid EventId { get; }
		DateTime OcurredOn { get; }
		void ApplyTo(IEventConsumer entity);
	}
}
