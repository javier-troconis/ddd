using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using shared;

namespace infra
{
    public static class StreamStateFolder
    {
        public static TState Fold<TState>(TState state, IEvent @event) where TState : IMessageHandler
        {
            var proxy = MessageHandlerProxy.TryCreate(state, @event, state);
            return proxy == null ? state : proxy.Handle(@event);
        }
    }
}
