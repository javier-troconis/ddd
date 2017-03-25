using infra;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using contracts;



using EventStore.ClientAPI;
using EventStore.ClientAPI.Common.Log;
using EventStore.ClientAPI.Exceptions;
using EventStore.ClientAPI.Projections;
using EventStore.ClientAPI.SystemData;

using ImpromptuInterface;



using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using shared;

namespace subscriber
{
	public class Program
	{
		private static Func<ResolvedEvent, Task<ResolvedEvent>> Enqueue(TaskQueue queue, Func<ResolvedEvent, Task<ResolvedEvent>> handle)
		{
			return async resolvedEvent =>
			{
				await queue.SendToChannelAsync(resolvedEvent.OriginalStreamId, () => handle(resolvedEvent));
				return resolvedEvent;
			};
		}

		private static Func<ResolvedEvent, Task<ResolvedEvent>> MakeIdempotent(ConcurrentDictionary<Guid, ResolvedEvent> handledMessages, Func<ResolvedEvent, Task<ResolvedEvent>> handle)
		{
			return resolvedEvent => handledMessages.TryAdd(resolvedEvent.Event.EventId, resolvedEvent) ? handle(resolvedEvent) : Task.FromResult(resolvedEvent);
		}

		public static void Main(string[] args)
		{
			var queue = new TaskQueue();
			var handledMessages = new ConcurrentDictionary<Guid, ResolvedEvent>();





			Task.WhenAll(Enumerable.Range(0, 1).Select(x =>
			{
				return new EventBus(
				EventStoreSettings.ClusterDns,
				EventStoreSettings.Username,
				EventStoreSettings.Password,
				EventStoreSettings.ExternalHttpPort,
				new ConsoleLogger())
				.RegisterCatchupSubscriber(
					new Subscriber3(),
					() => Task.FromResult(default(long?)),
					handle => Enqueue(queue, handle.ComposeForward(_writeCheckpoint.ToAsyncInput())))
				//.RegisterPersistentSubscriber(new Subscriber1(new EmailService().SendEmail), 
				//	handle => MakeIdempotent(handledMessages, handle))
				.Start();
			})).Wait();

			

			while (true)
			{
			}
		}



		private static readonly Func<ResolvedEvent, Task<ResolvedEvent>> _writeCheckpoint = resolvedEvent =>
		{
			Console.WriteLine("checkpointing - " + resolvedEvent.OriginalEventNumber);
			return Task.FromResult(resolvedEvent);
		};
	}
}
