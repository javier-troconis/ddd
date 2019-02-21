using System;
using System.Threading.Tasks;

using command.contracts;

using shared;
using eventstore;

namespace query
{
    //public class Subscriber1 :
    //    IMessageHandler<IRecordedEvent<IApplicationSubmittedV1>, Task>,
    //    IMessageHandler<IRecordedEvent<IApplicationStartedV2>, Task>
    //{
    //    public Task Handle(IRecordedEvent<IApplicationSubmittedV1> message)
    //    {
    //        Console.WriteLine($"{nameof(Subscriber1)} - {message.EventStreamId} - {nameof(IApplicationSubmittedV1)}");
    //        return Task.CompletedTask;
    //    }

    //    public Task Handle(IRecordedEvent<IApplicationStartedV2> message)
    //    {
    //        Console.WriteLine($"{nameof(Subscriber1)} - {message.EventStreamId} - {nameof(IApplicationStartedV2)}");
    //        return Task.CompletedTask;
    //    }
    //}
}
