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
			return ComposeForward1((dynamic)s1, (dynamic)s2);
		}

		public static Func<ResolvedEvent, Task<ResolvedEvent>> ComposeForward(this IMessageHandler s, Func<ResolvedEvent, Task<ResolvedEvent>> f)
		{
			return ComposeForward1((dynamic)s, f);
		}

		private static Func<ResolvedEvent, Task<ResolvedEvent>> ComposeForward1<TSubscriber1, TSubscriber2>(TSubscriber1 s1, TSubscriber2 s2) where TSubscriber1 : IMessageHandler where TSubscriber2 : IMessageHandler
		{
			var handleResolvedEvent = SubscriberResolvedEventHandleFactory.CreateSubscriberResolvedEventHandle<TSubscriber2, Task>(delegate { return Task.CompletedTask; });
			return ComposeForward1
			(
				s1,
				async resolvedEvent =>
				{
					await handleResolvedEvent(s2, resolvedEvent);
					return resolvedEvent;
				}
			);
		}

		private static Func<ResolvedEvent, Task<ResolvedEvent>> ComposeForward1<TSubscriber>(TSubscriber s, Func<ResolvedEvent, Task<ResolvedEvent>> f) where TSubscriber : IMessageHandler
		{
			var handleResolvedEvent = SubscriberResolvedEventHandleFactory.CreateSubscriberResolvedEventHandle<TSubscriber, Task>(delegate { return Task.CompletedTask; });
			return new Func<ResolvedEvent, Task<ResolvedEvent>>(
				async resolvedEvent =>
				{
					await handleResolvedEvent(s, resolvedEvent);
					return resolvedEvent;
				}).ComposeForward(f);
		}
	}


}
