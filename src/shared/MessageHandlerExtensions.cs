using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace shared
{
    public interface ICompositeMessageHandler<in TIn, out TOut> : IMessageHandler<TIn, TOut>
    {
        ICompositeMessageHandler<TIn, TOut1> ComposeForward<TOut1>(IMessageHandler<TOut, TOut1> to);
        ICompositeMessageHandler<TIn1, TOut> ComposeBackward<TIn1>(IMessageHandler<TIn1, TIn> @from);
    }

    public static class MessageHandlerExtensions
    {
        public static ICompositeMessageHandler<TIn, TOut1> ComposeForward<TIn, TOut, TOut1>(
            this IMessageHandler<TIn, TOut> @from, IMessageHandler<TOut, TOut1> to)
        {
            return new MessageHandler<TIn, TOut, TOut1>(@from.Handle, to.Handle);
        }

        public static ICompositeMessageHandler<TIn1, TOut> ComposeBackward<TIn1, TIn, TOut>(
            this IMessageHandler<TIn, TOut> to, IMessageHandler<TIn1, TIn> @from)
        {
            return new MessageHandler<TIn1, TIn, TOut>(@from.Handle, to.Handle);
        }

        private class MessageHandler<TIn, TInOut, TOut> : ICompositeMessageHandler<TIn, TOut>
        {
            private readonly Func<TIn, TInOut> _from;
            private readonly Func<TInOut, TOut> _to;

            public MessageHandler(Func<TIn, TInOut> from, Func<TInOut, TOut> to)
            {
                _from = from;
                _to = to;
            }

            public TOut Handle(TIn message)
            {
                return _from.ComposeForward(_to)(message);
            }

            public ICompositeMessageHandler<TIn, TOut1> ComposeForward<TOut1>(IMessageHandler<TOut, TOut1> to)
            {
                return new MessageHandler<TIn, TOut, TOut1>(Handle, to.Handle);
            }

            public ICompositeMessageHandler<TIn1, TOut> ComposeBackward<TIn1>(IMessageHandler<TIn1, TIn> @from)
            {
                return new MessageHandler<TIn1, TIn, TOut>(@from.Handle, Handle);
            }
        }

    }
}
