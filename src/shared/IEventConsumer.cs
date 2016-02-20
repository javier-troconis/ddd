namespace shared
{
	public interface IEventConsumer
	{

	}

	public interface IEventConsumer<in TEvent> : IEventConsumer where TEvent : Event
	{
		void Apply(TEvent @event);
	}
}
