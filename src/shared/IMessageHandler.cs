namespace shared
{
	
    public interface IMessageHandler
    {

    }

	public interface IMessageHandler<in TMessage, out TResult> : IMessageHandler where TMessage : IMessage
    {
        TResult Handle(TMessage message);
	}
}
