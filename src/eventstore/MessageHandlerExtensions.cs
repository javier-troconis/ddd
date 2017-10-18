﻿using System;
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
			return MessageHandlerExtensions.ComposeForward(s1, CreateSubscriberResolvedEventHandle((dynamic)s2));
		}

		public static Func<ResolvedEvent, Task<ResolvedEvent>> ComposeForward(this IMessageHandler s, Func<ResolvedEvent, Task<ResolvedEvent>> f)
		{
			return FuncExtensions.ComposeForward(CreateSubscriberResolvedEventHandle((dynamic)s), f);
		}

		internal static Func<ResolvedEvent, Task<ResolvedEvent>> CreateSubscriberResolvedEventHandle<TSubscriber>(this TSubscriber s) where TSubscriber : IMessageHandler
		{
			var handleEvent = SubscriberResolvedEventHandleFactory.CreateSubscriberResolvedEventHandle<TSubscriber, Task>(delegate { return Task.CompletedTask; });
			return async resolvedEvent =>
			{
				await handleEvent(s, resolvedEvent);
				return resolvedEvent;
			};
		}
	}


}
