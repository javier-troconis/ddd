namespace shared
{
	

	public interface IEventHandler<in TEvent, out TResult> where TEvent : IEvent
    {
        TResult Handle(TEvent @event);
	}
}
