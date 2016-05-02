using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace shared
{
    public static class EventFolder
    {
        public static TCandidateFolder Fold<TCandidateFolder>(TCandidateFolder candidateFolder, IEvent @event) where TCandidateFolder : IMessageHandler
        {
            return Fold(candidateFolder, (dynamic)@event);
        }

        private static TCandidateFolder Fold<TCandidateFolder, TEvent>(TCandidateFolder candidateFolder, TEvent @event) where TCandidateFolder : IMessageHandler where TEvent : IEvent
        {
            var folder = candidateFolder as IMessageHandler<TEvent, TCandidateFolder>;
            return folder == null ? candidateFolder : folder.Handle(@event);
        }
    }
}
