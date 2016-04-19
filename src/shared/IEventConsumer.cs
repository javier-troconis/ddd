namespace shared
{
	public interface IEventConsumer
	{

	}

	public interface IEventConsumer<in TEvent, out TEventConsumer> : IEventConsumer 
		where TEvent : IEvent 
		where TEventConsumer: IEventConsumer
	{
		TEventConsumer Apply(TEvent @event);
	}
}
