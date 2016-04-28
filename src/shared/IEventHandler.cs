namespace shared
{
	public interface IEventHandler
	{

	}

	public interface IEventHandler<in TEvent, out TResult> : IEventHandler where TEvent : IEvent
	{
        TResult Handle(TEvent @event);
	}
}
