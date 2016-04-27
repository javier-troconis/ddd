namespace shared
{
	public interface IEventHandler
	{

	}

	public interface IEventHandler<in TEvent, out TEventHandler> : IEventHandler where TEvent : IEvent where TEventHandler: IEventHandler
	{
		TEventHandler Handle(TEvent @event);
	}
}
