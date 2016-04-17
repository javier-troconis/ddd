namespace shared
{
	public interface IEventConsumer
	{

	}

	public interface IEventConsumer<in TEvent> : IEventConsumer where TEvent : IEvent
	{
		void Apply(TEvent @event);
	}
}
