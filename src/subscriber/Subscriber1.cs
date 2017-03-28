using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using core.contracts;

using shared;

namespace subscriber
{
    public class Subscriber1 : 
		IMessageHandler<IRecordedEvent<IApplicationSubmittedV1>, Task>
    {
	    public Task Handle(IRecordedEvent<IApplicationSubmittedV1> message)
	    {
			Console.WriteLine(typeof(IApplicationSubmittedV1).Name + " " + message.EventStreamId);
			return Task.CompletedTask;
		}
    }
}
