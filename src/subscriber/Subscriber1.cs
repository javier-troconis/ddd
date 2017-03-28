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
			var @event = message.Event;
			Console.WriteLine(@event.GetType().Name);
			return Task.CompletedTask;
		}
    }
}
