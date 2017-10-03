using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using shared;

namespace eventstore
{
	public abstract class Class2 : IMessageHandler
	{
		public static implicit operator Func<ResolvedEvent, Task<ResolvedEvent>>(Class2 x)
		{
			var handleResolvedEvent = x.CreateResolvedEventHandle(resolvedEvent => Task.CompletedTask);

			return
				async resolvedEvent =>
				{
					await handleResolvedEvent(resolvedEvent);
					return resolvedEvent;
				};
		}
	}
}
