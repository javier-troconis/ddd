namespace shared
{
    public interface IMessageHandler
    {

    }

	public interface IMessageHandler<in TMessage, out TResult> : IMessageHandler
    {
        TResult Handle(TMessage message);
	}
}
