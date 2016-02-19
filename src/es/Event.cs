using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace es
{
	public abstract class Event<TEvent> : IEvent where TEvent : Event<TEvent>
	{
		public Guid EventId { get; } = Guid.NewGuid();

		public DateTime OcurredOn { get; } = DateTime.UtcNow;

		public void ApplyTo(IEventSourcedEntity entity)
		{
			(entity as IEventSourcedEntity<TEvent>)?.Apply((TEvent)this);
		}
	}
}
