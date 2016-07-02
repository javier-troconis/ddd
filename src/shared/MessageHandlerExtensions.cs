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
            return new MessageHandler<TIn, TOut>(@from.Handle).ComposeForward(to);
        }

        public static ICompositeMessageHandler<TIn1, TOut> ComposeBackward<TIn1, TIn, TOut>(
            this IMessageHandler<TIn, TOut> to, IMessageHandler<TIn1, TIn> @from)
        {
            return @from.ComposeForward(to);
        }

        private class MessageHandler<TIn, TOut> : ICompositeMessageHandler<TIn, TOut>
        {
            private readonly Func<TIn, TOut> _handle;

            // take from, to : always compose forward
            public MessageHandler(Func<TIn, TOut> handle)
            {
                _handle = handle;
            }

            public TOut Handle(TIn message)
            {
                return _handle(message);
            }

            public ICompositeMessageHandler<TIn, TOut1> ComposeForward<TOut1>(IMessageHandler<TOut, TOut1> to)
            {
                return new MessageHandler<TIn, TOut1>(_handle.ComposeForward(to.Handle));
            }

            public ICompositeMessageHandler<TIn1, TOut> ComposeBackward<TIn1>(IMessageHandler<TIn1, TIn> @from)
            {
                return new MessageHandler<TIn1, TIn>(@from.Handle).ComposeForward(this);
            }
        }

    }
}
