using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace shared
{
    public static class EventDispatcher
    {
        public static TCandidateHandler Dispatch<TCandidateHandler>(TCandidateHandler candidateHandler, IEvent @event) where TCandidateHandler : IMessageHandler
        {
            return Dispatch(candidateHandler, (dynamic)@event);
        }

        private static TCandidateHandler Dispatch<TCandidateHandler, TEvent>(TCandidateHandler candidateHandler, TEvent @event) where TEvent : IEvent
        {
            var handler = candidateHandler as IMessageHandler<TEvent, TCandidateHandler>;
            return handler == null ? candidateHandler : handler.Handle(@event);
        }
    }
}
