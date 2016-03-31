using System;

namespace shared
{
	public interface IEvent
	{
		Guid EventId { get; }
		DateTime OcurredOn { get; }
	}

	public abstract class Event : IEvent
	{
		public Guid EventId { get; } = Guid.NewGuid();

		public DateTime OcurredOn { get; } = DateTime.UtcNow;

		public abstract void ApplyTo(IEventConsumer entity);
	}

	public abstract class Event<TEvent> : Event where TEvent : Event<TEvent>
	{
		public sealed override void ApplyTo(IEventConsumer entity)
		{
			(entity as IEventConsumer<TEvent>)?.Apply((TEvent)this);
		}
	}
}
