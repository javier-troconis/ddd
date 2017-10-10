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
		public static Func<ResolvedEvent, Task<ResolvedEvent>> ComposeForward<TSubscriber1, TSubscriber2>(this TSubscriber1 s1, TSubscriber2 s2) where TSubscriber1 : IMessageHandler where TSubscriber2 : IMessageHandler
		{
			var handleResolvedEvent = SubscriberResolvedEventHandleFactory.CreateSubscriberResolvedEventHandle<TSubscriber1, Task>(delegate { return Task.CompletedTask; });
			return ComposeForward
				(
					async resolvedEvent =>
					{
						await handleResolvedEvent(s1, resolvedEvent);
						return resolvedEvent;
					},
					s2
				);
		}

		public static Func<ResolvedEvent, Task<ResolvedEvent>> ComposeForward<TSubscriber>(this Func<ResolvedEvent, Task<ResolvedEvent>> f, TSubscriber s) where TSubscriber : IMessageHandler
		{
			var handleResolvedEvent = SubscriberResolvedEventHandleFactory.CreateSubscriberResolvedEventHandle<TSubscriber, Task>(delegate { return Task.CompletedTask; });
			return f.ComposeForward
				(async resolvedEvent =>
					{
						await handleResolvedEvent(s, resolvedEvent);
						return resolvedEvent;
					}
				);
		}
	}


}
