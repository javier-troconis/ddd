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

    public static class MessageHandlerComposer
    {
        public static ICompositeMessageHandler<TIn, TOut1> ComposeForward<TIn, TOut, TOut1>(
            IMessageHandler<TIn, TOut> @from, IMessageHandler<TOut, TOut1> to)
        {
            return new MessageHandler<TIn, TOut1>(new Func<TIn, TOut>(@from.Handle).ComposeForward(to.Handle));
        }

        public static ICompositeMessageHandler<TIn1, TOut> ComposeBackward<TIn1, TIn, TOut>(
            IMessageHandler<TIn, TOut> to, IMessageHandler<TIn1, TIn> @from)
        {
            return ComposeForward(@from, to);
        }

        private class MessageHandler<TIn, TOut> : ICompositeMessageHandler<TIn, TOut>
        {
            private readonly Func<TIn, TOut> _handle;

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
                return MessageHandlerComposer.ComposeForward(this, to);
            }

            public ICompositeMessageHandler<TIn1, TOut> ComposeBackward<TIn1>(IMessageHandler<TIn1, TIn> @from)
            {
                return MessageHandlerComposer.ComposeBackward(this, @from);
            }
        }
    }
}
