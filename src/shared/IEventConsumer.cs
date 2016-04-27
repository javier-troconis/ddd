namespace shared
{
	public interface IEventConsumer
	{

	}

	public interface IEventConsumer<in TEvent, out TEventConsumer> : IEventConsumer where TEvent : IEvent
	{
		TEventConsumer Apply(TEvent @event);
	}
}
