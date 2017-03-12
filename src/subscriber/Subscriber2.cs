using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using contracts;

using shared;

namespace subscriber
{
    public class Subscriber2 : IMessageHandler<IApplicationStartedV2, Task>
	{
		public Task Handle(IApplicationStartedV2 message)
		{
			Console.WriteLine(nameof(Subscriber2) + " " + typeof(IApplicationStartedV2));
			return Task.CompletedTask;
		}
	}
}
