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
	public enum ConnectionStatus
	{
		Connected,
		Disconnected
	}

	public struct SubscriberStatus
	{
		public readonly string Name;
		public readonly ConnectionStatus ConnectionStatus;

		public SubscriberStatus(string name, ConnectionStatus connectionStatus)
		{
			Name = name;
			ConnectionStatus = connectionStatus;
		}
	}

	public sealed class EventBus
	{
		private readonly Dictionary<string, Subscriber> _connectedSubscribers = new Dictionary<string, Subscriber>();
		private readonly TaskQueue _queue = new TaskQueue();
		private readonly Func<IEventStoreConnection> _createConnection;
		private readonly ReadOnlyDictionary<string, StartSubscriber> _subscriberRegistry;

		private EventBus(Func<IEventStoreConnection> createConnection, ReadOnlyDictionary<string, StartSubscriber> subscriberRegistry)
		{
			_createConnection = createConnection;
			_subscriberRegistry = subscriberRegistry;
		}

		public async Task<IEnumerable<SubscriberStatus>> StopSubscriber(string subscriberName)
		{
			var tsc = new TaskCompletionSource<IEnumerable<SubscriberStatus>>();
			await _queue.SendToChannel(
				() =>
				{
					if (_connectedSubscribers.TryGetValue(subscriberName, out Subscriber subscriber))
					{
						subscriber.Stop();
						_connectedSubscribers.Remove(subscriberName);
					}
					return Task.CompletedTask;
				}, 
				taskSucceeded: x => tsc.SetResult(GetSubscriberStatuses()));
			return await tsc.Task;
		}

		public async Task<IEnumerable<SubscriberStatus>> StopAllSubscribers()
		{
			var tcs = new TaskCompletionSource<IEnumerable<SubscriberStatus>>();
			await _queue.SendToChannel(
				() => 
				{
					_connectedSubscribers
						.ToList()
						.ForEach(
							x =>
							{
								x.Value.Stop();
								_connectedSubscribers.Remove(x.Key);
							});
					return Task.CompletedTask;
				}, 
				taskSucceeded: x => tcs.SetResult(GetSubscriberStatuses()));
			return await tcs.Task;
		}

		public async Task<IEnumerable<SubscriberStatus>> StartSubscriber(string subscriberName)
		{
			var tcs = new TaskCompletionSource<IEnumerable<SubscriberStatus>>();
			await _queue.SendToChannel(
				async () =>
				{
                    if (_subscriberRegistry.TryGetValue(subscriberName, out StartSubscriber startSubscriber) && !_connectedSubscribers.ContainsKey(subscriberName))
                    {
                        var subscriber = await startSubscriber(_createConnection);
                        _connectedSubscribers.Add(subscriberName, subscriber);
                    }
				},
				taskSucceeded: x => tcs.SetResult(GetSubscriberStatuses()));
			return await tcs.Task;
		}

		public async Task<IEnumerable<SubscriberStatus>> StartAllSubscribers()
		{
			var tcs = new TaskCompletionSource<IEnumerable<SubscriberStatus>>();
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
				},
				taskSucceeded: x => tcs.SetResult(GetSubscriberStatuses()));
			return await tcs.Task;
		}

		private IEnumerable<SubscriberStatus> GetSubscriberStatuses()
		{
			return new HashSet<SubscriberStatus>(
				_subscriberRegistry
					.Select(
						subscriberRegistration => new 
							SubscriberStatus
							(
								subscriberRegistration.Key,
								_connectedSubscribers.ContainsKey(subscriberRegistration.Key) ? ConnectionStatus.Connected : ConnectionStatus.Disconnected
							)));
		}

		public static EventBus CreateEventBus(Func<IEventStoreConnection> createConnection, Func<SubscriberRegistry, SubscriberRegistry> configureSubscribersRegistry)
		{
			var subscriberRegistry = configureSubscribersRegistry(SubscriberRegistry.CreateSubscriberRegistry());
			return new EventBus(createConnection, subscriberRegistry);
		}
	}
}
