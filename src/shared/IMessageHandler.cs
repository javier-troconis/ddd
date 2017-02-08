namespace shared
{
    public interface IMessageHandler<in TIn, out TOut>
    {
        TOut Handle(TIn message);
	}

    public interface IMessageHandler<in TIn>
    {
        void Handle(TIn message);
    }
}
