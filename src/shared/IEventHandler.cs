namespace shared
{
	public interface IEventHandler
	{

	}

	public interface IEventHandler<in TEvent, out TResult> : IEventHandler 
        where TEvent : IEvent where TResult : IEventHandler<TEvent, TResult>
    {
        TResult Handle(TEvent @event);
	}
}
