using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

using EventStore.ClientAPI;

using ImpromptuInterface;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using shared;

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

	//public sealed class EventBus1
	//{
	//	private readonly Func<IEventStoreConnection> _createConnection;
	//	private readonly SubscriberRegistry _subscriberRegistry;

	//	private EventBus1(
	//		Func<IEventStoreConnection> createConnection,
	//		SubscriberRegistry subscriberRegistry)
	//	{
	//		_createConnection = createConnection;
	//		_subscriberRegistry = subscriberRegistry;
	//	}

	//	public void StopSubscription(string subscriberName = "*")
	//	{
	//		//Parallel.ForEach(_subscriberRegistry, subscriber => subscriber.Stop());
	//	}

	//	public async Task StartSubscription(string subscriberName = "*")
	//	{
	//		await Task.WhenAll
	//			(
	//				_subscriberRegistry
	//					.Where(x => x.SubscriberName.MatchesWildcard(subscriberName))
	//					.Select(x =>
	//					{
	//						return x.StartSubscriber(_createConnection);
	//					})
	//			);
	//	}

	//	public static EventBus1 CreateEventBus(Func<IEventStoreConnection> createConnection, Func<SubscriberRegistry, SubscriberRegistry> registerSubscribers)
	//	{
	//		var subscriberRegistry = registerSubscribers(SubscriberRegistry.CreateSubscriberRegistry());
	//		return new EventBus1(createConnection, subscriberRegistry);
	//	}
	//}
}
