using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace shared
{
    public static class MessageHandlerDelegate
    {
        public static IMessageHandler<object, TOut> TryCreateFromCandidate<TIn, TOut>(IMessageHandler candidate, TIn sampleIn, TOut sampleOut)
        {
            var handler = candidate as IMessageHandler<TIn, TOut>;
            return Equals(handler, null) ? null : new MessageHandler<TIn, TOut>(handler);
        }

        private class MessageHandler<TIn, TOut> : IMessageHandler<object, TOut>
        {
            private readonly IMessageHandler<TIn, TOut> _handler;

            public MessageHandler(IMessageHandler<TIn, TOut> handler)
            {
                _handler = handler;
            }

            public TOut Handle(object message)
            {
                return _handler.Handle((TIn)message);
            }
        }
    }

}
