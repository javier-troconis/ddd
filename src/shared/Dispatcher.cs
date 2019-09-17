using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using eventstore;

namespace shared
{
    public static class Dispatcher
    {
        public static Task<T2> DispatchAsync<T1, T2>(IMessageHandler<T1, Task<T2>> messageHandler, byte[] messageData)
        {
            return Dispatch(messageHandler, messageData);
        }

        public static T2 Dispatch<T1, T2>(IMessageHandler<T1, T2> messageHandler, byte[] messageData)
        {
            var message = messageData.ParseJson<T1>();
            return messageHandler.Handle(message);
        }
    }
}
