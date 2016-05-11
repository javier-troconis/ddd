using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace shared
{
    public static class MessageHandlerExtensions
    {
        public static ICompositeMessageHandler<TIn, TOut1> ComposeForward<TIn, TOut, TOut1>(
            this IMessageHandler<TIn, TOut> @from, IMessageHandler<TOut, TOut1> to)
        {
            return MessageHandlerComposer.ComposeForward(@from, to);
        }

        public static ICompositeMessageHandler<TIn1, TOut> ComposeBackward<TIn1, TIn, TOut>(
            this IMessageHandler<TIn, TOut> to, IMessageHandler<TIn1, TIn> @from)
        {
            return MessageHandlerComposer.ComposeBackward(to, @from);
        }
    }
}
