using System;

namespace shared
{
    public interface IMessageHandler<in TIn, out TOut>
    {
        TOut Handle(TIn message);
	}
}
