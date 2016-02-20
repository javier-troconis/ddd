using System;

namespace shared
{
    public interface IEvent
    {
		Guid EventId { get; }
		DateTime OcurredOn { get; }
	}
}
