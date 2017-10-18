using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

using EventStore.ClientAPI;
using shared;
using ImpromptuInterface;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace eventstore
{
	public sealed class EventBus
	{
		private readonly IEnumerable<Subscriber> _subscribers;

		private EventBus(IEnumerable<Subscriber> subscribers)
		{
			_subscribers = subscribers;
		}

		public void Stop()
		{
			Parallel.ForEach(_subscribers, subscriber => subscriber.Stop());
		}

		public static async Task<EventBus> Start(Func<IEventStoreConnection> createConnection, Func<SubscriberRegistry, SubscriberRegistry> registerSubscribers)
		{
			var subscriberRegistry = registerSubscribers(SubscriberRegistry.CreateSubscriberRegistry());
			var subscribers = await Task.WhenAll(subscriberRegistry.Select(entry => entry.StartSubscriber(createConnection)));
			return new EventBus(subscribers);
		}
	}

	public sealed class EventBus1
	{
		private readonly List<KeyValuePair<string, Subscriber>> _connectedSubscribers = new List<KeyValuePair<string, Subscriber>>();
		private readonly TaskQueue _queue = new TaskQueue();
		private readonly Func<IEventStoreConnection> _createConnection;
		private readonly SubscriberRegistry _subscriberRegistry;

		private EventBus1(
			Func<IEventStoreConnection> createConnection,
			SubscriberRegistry subscriberRegistry)
		{
			_createConnection = createConnection;
			_subscriberRegistry = subscriberRegistry;
		}

		public async Task StopSubscriber(string subscriberName)
		{
			var tsc = new TaskCompletionSource<bool>();
			await _queue.SendToChannel(
				string.Empty,
				() => Task.Run(() =>
				{
					_connectedSubscribers
						.Where(x => x.Key == subscriberName)
						.ToList()
						.ForEach(
							x =>
							{
								x.Value.Stop();
								_connectedSubscribers.Remove(x);
							});
					tsc.SetResult(true);
				}));
			await tsc.Task;
		}

		public async Task StopAllSubscribers()
		{
			var tsc = new TaskCompletionSource<bool>();
			await _queue.SendToChannel(
				string.Empty,
				() => Task.Run(() =>
				{
					_connectedSubscribers
						.ForEach(
							x =>
							{
								x.Value.Stop();
								_connectedSubscribers.Remove(x);
							});
					tsc.SetResult(true);
				}));
			await tsc.Task;
		}

		public async Task StartSubscriber(string subscriberName)
		{
			var tcs = new TaskCompletionSource<bool>();
			await _queue.SendToChannel(
				string.Empty,
				async () =>
				{
					var subscribers =
						await Task.WhenAll
						(
							_subscriberRegistry.Where(x => x.SubscriberName == subscriberName)
								.Select
								(
									async x => new KeyValuePair<string, Subscriber>(x.SubscriberName, await x.StartSubscriber(_createConnection))
								)
						);
					_connectedSubscribers.AddRange(subscribers);
					tcs.SetResult(true);
				});
			await tcs.Task;
		}

		public async Task StartAllSubscribers()
		{
			var tcs = new TaskCompletionSource<bool>();
			await _queue.SendToChannel(
				string.Empty,
				async () =>
				{
					var subscribers =
						await Task.WhenAll
						(
							_subscriberRegistry
								.Select
								(
									async x => new KeyValuePair<string, Subscriber>(x.SubscriberName, await x.StartSubscriber(_createConnection))
								)
						);
					_connectedSubscribers.AddRange(subscribers);
					tcs.SetResult(true);
				});
			await tcs.Task;
		}

		public static EventBus1 CreateEventBus(Func<IEventStoreConnection> createConnection, Func<SubscriberRegistry, SubscriberRegistry> registerSubscribers)
		{
			var subscriberRegistry = registerSubscribers(SubscriberRegistry.CreateSubscriberRegistry());
			return new EventBus1(createConnection, subscriberRegistry);
		}
	}
}
