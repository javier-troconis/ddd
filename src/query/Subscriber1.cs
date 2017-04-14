using System;
using System.Threading.Tasks;

using command.contracts;

using shared;

namespace query
{
    public class Subscriber1 : 
		IMessageHandler<IRecordedEvent<IApplicationSubmittedV1>, Task>, 
		IMessageHandler<IRecordedEvent<IApplicationStartedV2>, Task>
    {
	    public Task Handle(IRecordedEvent<IApplicationSubmittedV1> message)
	    {
			Console.WriteLine($"{nameof(IApplicationSubmittedV1)} {message.EventStreamId}");
			return Task.CompletedTask;
		}

	    public Task Handle(IRecordedEvent<IApplicationStartedV2> message)
	    {
			//Console.WriteLine($"{nameof(IApplicationStartedV2)} {message.EventStreamId}");
			return Task.CompletedTask;
		}
    }
}
