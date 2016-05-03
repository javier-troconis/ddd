using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace shared
{
    public static class MessageHandlerExtensions
    {
        public static IMessageHandler<TMessage, TLastResult> ComposeForward<TMessage, TResult, TLastMessage, TLastResult>(
            this IMessageHandler<TMessage, TResult> handler, IMessageHandler<TLastMessage, TLastResult> lastHandler)
            where TResult : TLastMessage
        {
            return new CompositeMessageHandlerWrapper<TMessage, TLastResult>(message => lastHandler.Handle(handler.Handle(message)));
        }

        public static IMessageHandler<TLastMessage, TResult> ComposeBackwards<TMessage, TResult, TLastMessage, TLastResult>(
           this IMessageHandler<TMessage, TResult> handler, IMessageHandler<TLastMessage, TLastResult> lastHandler)
           where TLastResult : TMessage
        {
            return new CompositeMessageHandlerWrapper<TLastMessage, TResult>(message => handler.Handle(lastHandler.Handle(message)));
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
