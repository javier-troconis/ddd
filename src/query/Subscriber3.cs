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
			Console.WriteLine($"{nameof(IApplicationStartedV1)} {message.EventStreamId}");
			return Task.CompletedTask;
		}

    }
}
