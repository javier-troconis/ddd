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
using System.Collections.ObjectModel;

namespace eventstore
{
	public sealed class EventBus
	{
		private readonly Dictionary<string, Subscriber> _connectedSubscribers = new Dictionary<string, Subscriber>();
		private readonly TaskQueue _queue = new TaskQueue();
		private readonly Func<IEventStoreConnection> _createConnection;
		private readonly IReadOnlyDictionary<string, StartSubscriber> _subscriberRegistry;

		private EventBus(
			Func<IEventStoreConnection> createConnection,
            IReadOnlyDictionary<string, StartSubscriber> subscriberRegistry)
		{
			_createConnection = createConnection;
			_subscriberRegistry = subscriberRegistry;
		}

		public async Task StopSubscriber(string subscriberName)
		{
			var tsc = new TaskCompletionSource<bool>();
			await _queue.SendToChannel(
				() => Task.Run(() =>
				{
                    if (_connectedSubscribers.TryGetValue(subscriberName, out Subscriber subscriber))
                    {
                        subscriber.Stop();
                        _connectedSubscribers.Remove(subscriberName);
                    }
                    tsc.SetResult(true);
				}));
			await tsc.Task;
		}

		public async Task StopAllSubscribers()
		{
			var tsc = new TaskCompletionSource<bool>();
			await _queue.SendToChannel(
				() => Task.Run(() =>
				{
					_connectedSubscribers
                        .ToList()
						.ForEach(
							x =>
							{
								x.Value.Stop();
								_connectedSubscribers.Remove(x.Key);
							});
					tsc.SetResult(true);
				}));
			await tsc.Task;
		}

		public async Task StartSubscriber(string subscriberName)
		{
			var tcs = new TaskCompletionSource<bool>();
			await _queue.SendToChannel(
				async () =>
				{
                    if (_subscriberRegistry.TryGetValue(subscriberName, out StartSubscriber startSubscriber) && !_connectedSubscribers.ContainsKey(subscriberName))
                    {
                        var subscriber = await startSubscriber(_createConnection);
                        _connectedSubscribers.Add(subscriberName, subscriber);
                    }
                    tcs.SetResult(true);
				});
			await tcs.Task;
		}

		public async Task StartAllSubscribers()
		{
			var tcs = new TaskCompletionSource<bool>();
			await _queue.SendToChannel(
				async () =>
				{
                    var notConnectedSubscriberNames = _subscriberRegistry.Select(x => x.Key).Except(_connectedSubscribers.Select(x => x.Key));
                    await Task.WhenAll
                    (
                        notConnectedSubscriberNames
                            .Select
                            (
                                async subscriberName => 
                                {
                                    var subscriber = await _subscriberRegistry[subscriberName](_createConnection);
                                    _connectedSubscribers.Add(subscriberName, subscriber);
                                }
                            )
                    );
					tcs.SetResult(true);
				});
			await tcs.Task;
		}

		public static EventBus CreateEventBus(Func<IEventStoreConnection> createConnection, Func<SubscriberRegistry, ReadOnlyDictionary<string, StartSubscriber>> registerSubscribers)
		{
            var subscriberRegistry = registerSubscribers(SubscriberRegistry.CreateSubscriberRegistry());
			return new EventBus(createConnection, subscriberRegistry);
		}
	}
}
