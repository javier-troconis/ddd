using System;

namespace shared
{
    public interface IMessageHandler
    {
        
    }

    public interface IMessageHandler<in TIn, out TOut> : IMessageHandler
    {
        TOut Handle(TIn message);
	}
}
