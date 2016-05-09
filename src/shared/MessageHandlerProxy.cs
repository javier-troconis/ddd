using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace shared
{
    public static class MessageHandlerProxy
    {
        public static IMessageHandler<TIn, TOut> TryCreate<TIn, TOut>(IMessageHandler candidate, TIn @in, TOut @out)
        {
            return TryCreate(candidate, (dynamic)@in, @in, @out);
        }

        private static IMessageHandler<TIn, TOut> TryCreate<TIn, TSpecificIn, TOut>(IMessageHandler candidate, TSpecificIn specificIn, TIn @in, TOut @out) 
            where TSpecificIn : TIn
        {
            var handler = candidate as IMessageHandler<TSpecificIn, TOut>;
            return Equals(handler, null) ? null : new MessageHandler<TSpecificIn, TIn, TOut>(handler);
        }

        private class MessageHandler<TIn1, TIn, TOut> : IMessageHandler<TIn, TOut> where TIn1 : TIn
        {
            private readonly IMessageHandler<TIn1, TOut> _handler;

            public MessageHandler(IMessageHandler<TIn1, TOut> handler)
            {
                _handler = handler;
            }

            public TOut Handle(TIn message)
            {
                return _handler.Handle((TIn1)message);
            }
        }
    }

}
