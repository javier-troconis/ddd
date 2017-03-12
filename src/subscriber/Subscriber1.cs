using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using contracts;

using shared;

namespace subscriber
{
    public class Subscriber1 : IMessageHandler<IApplicationStartedV1, Task>
	{
	    public Task Handle(IApplicationStartedV1 message)
	    {
		    Console.WriteLine(nameof(Subscriber1) + " " + typeof(IApplicationStartedV1));
		    return Task.CompletedTask;
	    }
	}
}
