using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using contracts;

using shared;

namespace subscriber
{
    public class Subscriber3 : 
		IMessageHandler<IApplicationStartedV3, Task>, 
		IMessageHandler<IApplicationStartedV2, Task>, 
		IMessageHandler<IApplicationStartedV1, Task>
	{
		public Task Handle(IApplicationStartedV3 message)
		{
			Console.WriteLine(nameof(Subscriber3) + " " + typeof(IApplicationStartedV3));
			return Task.CompletedTask;
		}

	    public Task Handle(IApplicationStartedV2 message)
	    {
			Console.WriteLine(nameof(Subscriber3) + " " + typeof(IApplicationStartedV2));
			return Task.CompletedTask;
		}

	    public Task Handle(IApplicationStartedV1 message)
	    {
			Console.WriteLine(nameof(Subscriber3) + " " + typeof(IApplicationStartedV1));
			return Task.CompletedTask;
		}
	}
}
