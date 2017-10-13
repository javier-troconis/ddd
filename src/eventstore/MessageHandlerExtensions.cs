using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using ImpromptuInterface;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using shared;

namespace eventstore
{
	public static class MessageHandlerExtensions
	{
		public static Func<ResolvedEvent, Task<ResolvedEvent>> ComposeForward(this IMessageHandler s1, IMessageHandler s2)
		{
			var s2Handle = CreateSubscriberResolvedEventHandle((dynamic) s2);
			return MessageHandlerExtensions.ComposeForward
			(
				s1,
				s2Handle
			);
		}

		public static Func<ResolvedEvent, Task<ResolvedEvent>> ComposeForward(this IMessageHandler s, Func<ResolvedEvent, Task<ResolvedEvent>> f)
		{
			var sHandle = CreateSubscriberResolvedEventHandle((dynamic) s);
			return FuncExtensions.ComposeForward(sHandle, f);
		}

		private static Func<ResolvedEvent, Task<ResolvedEvent>> CreateSubscriberResolvedEventHandle<TSubscriber>(TSubscriber s) where TSubscriber : IMessageHandler
		{
			var handleResolvedEvent = SubscriberResolvedEventHandleFactory.CreateSubscriberResolvedEventHandle<TSubscriber, Task>(delegate { return Task.CompletedTask; });
			return async resolvedEvent =>
			{
				await handleResolvedEvent(s, resolvedEvent);
				return resolvedEvent;
			};
		}
	}


}
