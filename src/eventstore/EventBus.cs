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
			var createSubscribers = registerSubscribers(SubscriberRegistry.Create());
			var subscribers = await Task.WhenAll(createSubscribers.Select(createSubscriber => createSubscriber(createConnection)));
			return new EventBus(subscribers);
		}
	}
}
