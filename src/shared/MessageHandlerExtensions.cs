using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace shared
{
    public static class MessageHandlerExtensions
    {
        public static IMessageHandler<Task<T1>, Task<T2>> ToTaskOfInput<T1, T2>(this IMessageHandler<T1, Task<T2>> f)
        {
            Func<T1, Task<T2>> handle = f.Handle;
            return new X<Task<T1>, Task<T2>>(handle.ToTaskOfInput());
        }

        public static IMessageHandler<Task<T1>, Task<T2>> ToTaskOfInput<T1, T2>(this IMessageHandler<T1, T2> f)
        {
            Func<T1, T2> handle = f.Handle;
            return new X<Task<T1>, Task<T2>>(handle.ToTaskOfInput());
        }

        public static IMessageHandler<T1, T3> ComposeForward<T1, T2, T3>(this IMessageHandler<T1, T2> f1, IMessageHandler<T2, T3> f2)
        {
            Func<T1, T2> f1Handle = f1.Handle;
            return new X<T1, T3>(f1Handle.ComposeForward(f2.Handle));
        }

        public static IMessageHandler<T1, Task<T3>> ComposeForward<T1, T2, T3>(this IMessageHandler<T1, Task<T2>> f1, IMessageHandler<T2, T3> f2)
        {
            Func<T1, Task<T2>> f1Handle = f1.Handle;
            Func<T2, T3> f2Handle = f2.Handle;
            return new X<T1, Task<T3>>(f1Handle.ComposeForward(f2Handle.ToTaskOfInput()));
        }

        private class X<T1, T2> : IMessageHandler<T1, T2>
        {
            private readonly Func<T1, T2> _f;

            public X(Func<T1, T2> f)
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
