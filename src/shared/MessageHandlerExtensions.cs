using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace shared
{
    //public interface ICompositeMessageHandler<in T1, out T2> : IMessageHandler<T1, T2>
    //{
    //    ICompositeMessageHandler<T1, T3> ComposeForward<T3>(IMessageHandler<T2, T3> to);
    //    ICompositeMessageHandler<T4, T2> ComposeBackward<T4>(IMessageHandler<T4, T1> @from);
    //}

    //public static class MessageHandlerExtensions
    //{
    //    public static TSubscriber Apply<TSubscriber>(this TSubscriber subscriber, object message) where TSubscriber : IMessageHandler
    //    {
    //        return Apply(subscriber, (dynamic)message);
    //    }

    //    private static TSubscriber Apply<TSubscriber, TMessage>(TSubscriber subscriber, TMessage message)
    //    {
    //        var handler = subscriber as IMessageHandler<TMessage, TSubscriber>;
    //        return Equals(handler, null) ? subscriber : handler.Handle(message);
    //    }

    //    public static ICompositeMessageHandler<T1, T3> ComposeForward<T1, T2, T3>(this IMessageHandler<T1, T2> @from, IMessageHandler<T2, T3> to)
    //    {
    //        return new CompositeMessageHandler<T1, T2, T3>(@from.Handle, to.Handle);
    //    }

    //    public static ICompositeMessageHandler<T1, T3> ComposeBackward<T1, T2, T3>(this IMessageHandler<T2, T3> to, IMessageHandler<T1, T2> @from)
    //    {
    //        return new CompositeMessageHandler<T1, T2, T3>(@from.Handle, to.Handle);
    //    }

    //    private class CompositeMessageHandler<T1, T2, T3> : ICompositeMessageHandler<T1, T3>
    //    {
    //        private readonly Func<T1, T2> _from;
    //        private readonly Func<T2, T3> _to;

    //        public CompositeMessageHandler(Func<T1, T2> from, Func<T2, T3> to)
    //        {
    //            _from = from;
    //            _to = to;
    //        }

    //        public T3 Handle(T1 message)
    //        {
    //            return _from.ComposeForward(_to)(message);
    //        }

    //        public ICompositeMessageHandler<T1, T4> ComposeForward<T4>(IMessageHandler<T3, T4> to)
    //        {
    //            return new CompositeMessageHandler<T1, T3, T4>(Handle, to.Handle);
    //        }

    //        public ICompositeMessageHandler<T5, T3> ComposeBackward<T5>(IMessageHandler<T5, T1> @from)
    //        {
    //            return new CompositeMessageHandler<T5, T1, T3>(@from.Handle, Handle);
    //        }
    //    }

    //}
}
