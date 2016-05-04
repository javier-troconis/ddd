using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace shared
{
    public static class MessageHandlerExtensions
    {
        public static IMessageHandler<TFromMessage, TToResult> ComposeForward<TFromMessage, TFromResult, TToMessage, TToResult>(
          this IMessageHandler<TFromMessage, TFromResult> @from, IMessageHandler<TToMessage, TToResult> to)
          where TFromResult : TToMessage
        {
            return new CompositeMessageHandlerWrapper<TFromMessage, TToResult>(message => to.Handle(@from.Handle(message)));
        }

        public static IMessageHandler<TFromMessage, TToResult> ComposeBackward<TToMessage, TToResult, TFromMessage, TFromResult>(
           this IMessageHandler<TToMessage, TToResult> to, IMessageHandler<TFromMessage, TFromResult> @from)
           where TFromResult : TToMessage
        {
            return new CompositeMessageHandlerWrapper<TFromMessage, TToResult>(message => to.Handle(@from.Handle(message)));
        }

        private class CompositeMessageHandlerWrapper<TMessage, TResult> : IMessageHandler<TMessage, TResult>
        {
            private readonly Func<TMessage, TResult> _handler;

            public CompositeMessageHandlerWrapper(Func<TMessage, TResult> handler)
            {
                _handler = handler;
            }

            public TResult Handle(TMessage message)
            {
                return _handler(message);
            }
        }
    }
}
