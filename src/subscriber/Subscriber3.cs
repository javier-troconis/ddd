using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using core.contracts;

using shared;

namespace subscriber
{
    public class Subscriber3 : 
		IMessageHandler<IRecordedEvent<IApplicationStartedV1>, Task>
    {
	    public Task Handle(IRecordedEvent<IApplicationStartedV1> message)
	    {
			Console.WriteLine($"{nameof(Subscriber3)} {nameof(IApplicationStartedV1)} {message.EventStreamId}");
			return Task.CompletedTask;
		}

    }
}
