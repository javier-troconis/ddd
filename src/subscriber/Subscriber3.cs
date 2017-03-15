using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using contracts;

using shared;

namespace subscriber
{
    public class Subscriber3 : 
		IMessageHandler<IRecordedEvent<IApplicationStartedV1>, Task>,
		IMessageHandler<IRecordedEvent<IApplicationSubmittedV1>, Task>
    {


	    public Task Handle(IRecordedEvent<IApplicationStartedV1> message)
	    {
			Console.WriteLine($"{nameof(IApplicationStartedV1)} - {message.EventId} - {message.EventNumber}");
			return Task.CompletedTask;
		}

	    public Task Handle(IRecordedEvent<IApplicationSubmittedV1> message)
	    {
		    var @event = message.Event;
			Console.WriteLine($"{nameof(IApplicationSubmittedV1)} - {message.EventId} - {message.EventNumber} - {@event.SubmittedBy}");
			return Task.CompletedTask;
		}
    }
}
