using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace shared
{
    //public static class MessageHandlerExtensions
    //{
    //    public static IMessageHandler<TMessage, TNextResult> Compose<TMessage, TResult, TNextMessage, TNextResult>(
    //        this IMessageHandler<TMessage, TResult> handler, IMessageHandler<TNextMessage, TNextResult> nextHandler) 
    //        where TResult : TNextMessage
    //    {
    //        return new CompositeHandler<TMessage, TResult, TNextMessage, TNextResult>(handler, nextHandler);
    //    }

    //    private class CompositeHandler<TMessage, TResult, TNextMessage, TNextResult> : IMessageHandler<TMessage, TNextResult> where TResult : TNextMessage
    //    {
    //        private readonly IMessageHandler<TMessage, TResult> _handler;
    //        private readonly IMessageHandler<TNextMessage, TNextResult> _nextHandler;

    //        public CompositeHandler(IMessageHandler<TMessage, TResult> handler, IMessageHandler<TNextMessage, TNextResult> nextHandler)
    //        {
    //            _handler = handler;
    //            _nextHandler = nextHandler;
    //        }

    //        public TNextResult Handle(TMessage message)
    //        {
    //            return _nextHandler.Handle(_handler.Handle(message));
    //        }
    //    }
    //}

    public static class MessageHandlerExtensions
    {
        public static IMessageHandler<TMessage, TLastResult> ComposeForward<TMessage, TResult, TLastMessage, TLastResult>(
            this IMessageHandler<TMessage, TResult> handler, IMessageHandler<TLastMessage, TLastResult> lastHandler)
            where TResult : TLastMessage
        {
            return new CompositeHandlerWrapper<TMessage, TLastResult>(message => lastHandler.Handle(handler.Handle(message)));
        }

        public static IMessageHandler<TLastMessage, TResult> ComposeBackwards<TMessage, TResult, TLastMessage, TLastResult>(
           this IMessageHandler<TMessage, TResult> handler, IMessageHandler<TLastMessage, TLastResult> lastHandler)
           where TLastResult : TMessage
        {
            return new CompositeHandlerWrapper<TLastMessage, TResult>(message => handler.Handle(lastHandler.Handle(message)));
        }

        private class CompositeHandlerWrapper<TMessage, TResult> : IMessageHandler<TMessage, TResult>
        {
            private readonly Func<TMessage, TResult> _handler;

            public CompositeHandlerWrapper(Func<TMessage, TResult> handler)
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
