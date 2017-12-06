using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace shared
{
    public static class MessageHandlerExtensions
    {
        public static IMessageHandler<Task<T1>, Task<T2>> ToAsyncInput<T1, T2>(this IMessageHandler<T1, Task<T2>> f)
        {
            Func<T1, Task<T2>> handle = f.Handle;
            return new MessageHandler<Task<T1>, Task<T2>>(handle.Map());
        }

        public static IMessageHandler<Task<T1>, Task<T2>> ToAsyncInput<T1, T2>(this IMessageHandler<T1, T2> f)
        {
            Func<T1, T2> handle = f.Handle;
            return new MessageHandler<Task<T1>, Task<T2>>(handle.Map());
        }

        public static IMessageHandler<T1, T3> ComposeForward<T1, T2, T3>(this IMessageHandler<T1, T2> from, IMessageHandler<T2, T3> to)
        {
	        Func<T1, T2> handle = from.Handle;
			return new MessageHandler<T1, T3>(handle.ComposeForward(to.Handle));
        }

        public static IMessageHandler<T1, T3> ComposeBackward<T1, T2, T3>(this IMessageHandler<T2, T3> to, IMessageHandler<T1, T2> from)
        {
            return ComposeForward(from, to);
        }

        private class MessageHandler<T1, T2> : IMessageHandler<T1, T2>
        {
            private readonly Func<T1, T2> _f;

            public MessageHandler(Func<T1, T2> f)
            {
                _f = f;
            }

            public T2 Handle(T1 message)
            {
                return _f(message);
            }
        }
    }
}
