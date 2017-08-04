using System;
using System.Threading.Tasks;

using command.contracts;

using shared;

namespace query
{
    public class Subscriber3 : 
		IMessageHandler<IRecordedEvent<IApplicationStartedV1>, Task>
    {
	    public Task Handle(IRecordedEvent<IApplicationStartedV1> message)
	    {
            Console.WriteLine($"{nameof(Subscriber3)} - {nameof(IApplicationStartedV1)} {message.EventId}");
            return Task.CompletedTask;
		}

    }
}
