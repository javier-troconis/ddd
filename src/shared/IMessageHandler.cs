using System;

namespace shared
{
    public interface IMessageHandler
    {
        
    }

    public interface IMessageHandler<in T1, out T2> : IMessageHandler
    {
        T2 Handle(T1 message);
	}
}
