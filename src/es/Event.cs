using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace es
{
	public abstract class Event : IEvent
	{
		public Guid EventId { get; } = Guid.NewGuid();

		public DateTime OcurredOn { get; } = DateTime.UtcNow;

		public abstract void ApplyTo(IEventConsumer entity);
	}

	public abstract class Event<TEvent> : Event where TEvent : Event<TEvent>
	{
		public override sealed void ApplyTo(IEventConsumer entity)
		{
			(entity as IEventConsumer<TEvent>)?.Apply((TEvent)this);
		}
	}
}
