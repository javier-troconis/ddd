using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using contracts;

using shared;

namespace subscriber
{
    public class Subscriber1 : 
	
		IMessageHandler<IRecordedEvent<IApplicationSubmittedV1>, Task>
    {
	    public Task Handle(IRecordedEvent<IApplicationSubmittedV1> message)
	    {
			var @event = message.Event;
			Console.WriteLine($"{nameof(IApplicationSubmittedV1)} - {message.EventId} - {message.EventNumber} - {@event.SubmittedBy}");
			return Task.CompletedTask;
		}
    }
}
