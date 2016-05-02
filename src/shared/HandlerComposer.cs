using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace shared
{
    //public interface ICompositeHandler<TNextMessage, TNextResult> : IMessageHandler<TNextMessage, TNextResult>
    //{
    //    IMessageHandler<TNextMessage, TNextResult> Compose(IMessageHandler<TNextMessage, TNextResult> nextHandler);
    //}

    public static class HandlerComposer
    {
        public static IMessageHandler<TMessage, TNextResult> Create<TMessage, TResult, TNextMessage, TNextResult>(
            IMessageHandler<TMessage, TResult> handler,
            IMessageHandler<TNextMessage, TNextResult> nextHandler) where TResult : TNextMessage
        {
            return new CompositeHandler<TMessage, TResult, TNextMessage, TNextResult>(handler, nextHandler);
        }

        private class CompositeHandler<TMessage, TResult, TNextMessage, TNextResult> : 
            IMessageHandler<TMessage, TNextResult> where TResult : TNextMessage
        {
            private readonly IMessageHandler<TMessage, TResult> _handler;
            private readonly IMessageHandler<TNextMessage, TNextResult> _nextHandler;

            public CompositeHandler(IMessageHandler<TMessage, TResult> handler, IMessageHandler<TNextMessage, TNextResult> nextHandler)
            {
                _handler = handler;
                _nextHandler = nextHandler;
            }

            public TNextResult Handle(TMessage message)
            {
                return _nextHandler.Handle(_handler.Handle(message));
            }
        }
    }
}
