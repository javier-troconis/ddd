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
            return new CompositeMessageHandler<TIn, TOut, TOut1>(@from, to);
        }

        public static ICompositeMessageHandler<TIn1, TOut> ComposeBackward<TIn1, TIn, TOut>(
            IMessageHandler<TIn, TOut> to, IMessageHandler<TIn1, TIn> @from)
        {
            return new CompositeMessageHandler<TIn1, TIn, TOut>(@from, to);
        }

        private class CompositeMessageHandler<TIn, TInOut, TOut> : ICompositeMessageHandler<TIn, TOut>
        {
            private readonly IMessageHandler<TIn, TInOut> _from;
            private readonly IMessageHandler<TInOut, TOut> _to;

            public CompositeMessageHandler(IMessageHandler<TIn, TInOut> @from, IMessageHandler<TInOut, TOut> to)
            {
                _to = to;
                _from = @from;
            }

            public TOut Handle(TIn message)
            {
                return _to.Handle(_from.Handle(message));
            }

            public ICompositeMessageHandler<TIn, TOut1> ComposeForward<TOut1>(IMessageHandler<TOut, TOut1> to)
            {
                return new CompositeMessageHandler<TIn, TOut, TOut1>(this, to);
            }

            public ICompositeMessageHandler<TIn1, TOut> ComposeBackward<TIn1>(IMessageHandler<TIn1, TIn> @from)
            {
                return new CompositeMessageHandler<TIn1, TIn, TOut>(@from, this);
            }
        }

    }
}
