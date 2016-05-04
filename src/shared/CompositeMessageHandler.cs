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
            return new CompositeMessageHandler<TFromMessage, TToResult>(message => to.Handle(@from.Handle(message)));
        }

        public static ICompositeMessageHandler<TFromMessage, TToResult> ComposeBackward<TFromMessage, TFromResult, TToResult>(
            IMessageHandler<TFromResult, TToResult> to, IMessageHandler<TFromMessage, TFromResult> @from)
        {
            return ComposeForward(@from, to);
        }

        private class CompositeMessageHandler<TMessage, TResult> : ICompositeMessageHandler<TMessage, TResult>
        {
            private readonly Func<TMessage, TResult> _handler;

            public CompositeMessageHandler(Func<TMessage, TResult> handler)
            {
                _handler = handler;
            }

            public TResult Handle(TMessage message)
            {
                return _handler(message);
            }

            public ICompositeMessageHandler<TMessage, TToResult> ComposeForward<TToResult>(IMessageHandler<TResult, TToResult> to)
            {
                return MessageHandlerComposer.ComposeForward(this, to);
            }

            public ICompositeMessageHandler<TFromMessage, TResult> ComposeBackward<TFromMessage>(IMessageHandler<TFromMessage, TMessage> @from)
            {
                return MessageHandlerComposer.ComposeBackward(this, from);
            }
        }

    }
}
