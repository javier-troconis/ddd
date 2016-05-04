using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace shared
{
    public interface ICompositeMessageHandler<in TMessage, out TResult> : IMessageHandler<TMessage, TResult>
    {
        ICompositeMessageHandler<TMessage, TToResult> ComposeForward<TToResult>(IMessageHandler<TResult, TToResult> to);
        ICompositeMessageHandler<TFromMessage, TResult> ComposeBackward<TFromMessage>(IMessageHandler<TFromMessage, TMessage> @from);
    }

    public static class MessageHandlerComposer
    {
        public static ICompositeMessageHandler<TFromMessage, TToResult> ComposeForward<TFromMessage, TFromResult, TToResult>(
            IMessageHandler<TFromMessage, TFromResult> @from, IMessageHandler<TFromResult, TToResult> to)
        {
            return new CompositeMessageHandler<TFromMessage, TFromResult, TToResult>(@from, to);
        }

        public static ICompositeMessageHandler<TFromMessage, TToResult> ComposeBackward<TFromMessage, TFromResult, TToResult>(
            IMessageHandler<TFromResult, TToResult> to, IMessageHandler<TFromMessage, TFromResult> @from)
        {
            return new CompositeMessageHandler<TFromMessage, TFromResult, TToResult>(@from, to);
        }

        private class CompositeMessageHandler<TMessage, TMiddleResult, TResult> : ICompositeMessageHandler<TMessage, TResult>
        {
            private readonly IMessageHandler<TMessage, TMiddleResult> _from;
            private readonly IMessageHandler<TMiddleResult, TResult> _to;

            public CompositeMessageHandler(IMessageHandler<TMessage, TMiddleResult> @from, IMessageHandler<TMiddleResult, TResult> to)
            {
                _to = to;
                _from = @from;
            }

            public TResult Handle(TMessage message)
            {
                return _to.Handle(_from.Handle(message));
            }

            public ICompositeMessageHandler<TMessage, TToResult> ComposeForward<TToResult>(IMessageHandler<TResult, TToResult> to)
            {
                return new CompositeMessageHandler<TMessage, TResult, TToResult>(this, to);
            }

            public ICompositeMessageHandler<TFromMessage, TResult> ComposeBackward<TFromMessage>(IMessageHandler<TFromMessage, TMessage> @from)
            {
                return new CompositeMessageHandler<TFromMessage, TMessage, TResult>(@from, this);
            }
        }

    }
}
