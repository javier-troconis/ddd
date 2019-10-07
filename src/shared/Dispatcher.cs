using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using eventstore;

namespace shared
{
    public static class Dispatcher
    {
        public static T2 Dispatch<T1, T2>(IMessageHandler<T1, T2> handler, byte[] message)
        {
            return handler.Handle(message.ParseJson<T1>());
        }
    }
}
