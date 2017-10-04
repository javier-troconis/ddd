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
			return CreateSubscriberResolvedEventHandle((dynamic)x);
		}

		private static Func<ResolvedEvent, Task<ResolvedEvent>> CreateSubscriberResolvedEventHandle<TSubscriber>(TSubscriber subscriber) where TSubscriber : IMessageHandler
		{
			var handleResolvedEvent = MessageHandlerExtensions.CreateSubscriberResolvedEventHandle<TSubscriber, Task>(delegate { return Task.CompletedTask; });
			return
				async resolvedEvent =>
				{
					await handleResolvedEvent(subscriber, resolvedEvent);
					return resolvedEvent;
				};
		}
	}
}
