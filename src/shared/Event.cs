using System;

namespace shared
{
	public interface IEvent
	{

	}

	public abstract class Event : ValueObject<Event>, IEvent
	{
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
