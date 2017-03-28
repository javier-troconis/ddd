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
			Console.WriteLine($"{nameof(IApplicationStartedV1)} - {message.EventId} - {message.EventNumber}");
			return Task.CompletedTask;
		}

    }
}
