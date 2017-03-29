using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using core.contracts;

using shared;

namespace subscriber
{
    public class Subscriber1 : 
		IMessageHandler<IRecordedEvent<IApplicationSubmittedV1>, Task>, 
		IMessageHandler<IRecordedEvent<IApplicationStartedV2>, Task>
    {
	    public Task Handle(IRecordedEvent<IApplicationSubmittedV1> message)
	    {
			Console.WriteLine($"{nameof(Subscriber1)} {nameof(IApplicationSubmittedV1)} {message.EventStreamId}");
			return Task.CompletedTask;
		}

	    public Task Handle(IRecordedEvent<IApplicationStartedV2> message)
	    {
			Console.WriteLine($"{nameof(Subscriber1)} {nameof(IApplicationStartedV2)} {message.EventStreamId}");
			return Task.CompletedTask;
		}
    }
}
