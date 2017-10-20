using System;
using System.Threading.Tasks;

using command.contracts;

using shared;
using eventstore;

namespace query
{
    public class Subscriber3 :
        IMessageHandler<IRecordedEvent<IApplicationStartedV1>, Task>
    {
        public Task Handle(IRecordedEvent<IApplicationStartedV1> message)
        {
            Console.WriteLine($"{nameof(Subscriber3)} - {message.Header.EventStreamId} - {nameof(IApplicationStartedV1)} {message.Header.EventId}");
            return Task.CompletedTask;
        }

    }
}
