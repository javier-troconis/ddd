using System;
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
	public class EventBusHandle2
	{
		public readonly Action Stop;

		internal EventBusHandle2(Action stop)
		{
			Stop = stop;
		}
	}

	public sealed class EventBus2
	{
		private readonly IEnumerable<Func<Task<Subscriber>>> _subscriberStarters;
		private readonly Func<IEventStoreConnection> _createConnection;

		public EventBus2(Func<IEventStoreConnection> createConnection) : this(createConnection, Enumerable.Empty<Func<Task<Subscriber>>>())
		{

		}

		private EventBus2(Func<IEventStoreConnection> createConnection, IEnumerable<Func<Task<Subscriber>>> subscriberStarters)
		{
			_createConnection = createConnection;
			_subscriberStarters = subscriberStarters;
		}

		public EventBus2 RegisterCatchupSubscriber<TSubscriber>(TSubscriber subscriber, Func<Task<long?>> getCheckpoint, Func<ResolvedEvent, string> getEventHandlingQueueKey = null) where TSubscriber : IMessageHandler
		{
			return RegisterCatchupSubscriber<TSubscriber, TSubscriber>(subscriber, getCheckpoint, getEventHandlingQueueKey);

		}

		public EventBus2 RegisterCatchupSubscriber<TSubscription, TSubscriber>(TSubscriber subscriber, Func<Task<long?>> getCheckpoint, Func<ResolvedEvent, string> getEventHandlingQueueKey = null) where TSubscription : IMessageHandler where TSubscriber : TSubscription
		{
			return RegisterCatchupSubscriber<TSubscription>
			(
				SubscriberResolvedEventHandleFactory
					.CreateSubscriberResolvedEventHandle<TSubscriber, Task>(delegate { return Task.CompletedTask; })
					.Partial(subscriber),
				getCheckpoint,
				getEventHandlingQueueKey
			);
		}

		public EventBus2 RegisterCatchupSubscriber<TSubscription>(Func<ResolvedEvent, Task> handleResolvedEvent, Func<Task<long?>> getCheckpoint, Func<ResolvedEvent, string> getEventHandlingQueueKey = null) where TSubscription : IMessageHandler
		{
			return new EventBus2(_createConnection,
				_subscriberStarters.Concat(new Func<Task<Subscriber>>[]
				{
					() => 
						Subscriber.StartCatchUpSubscriber(
							_createConnection, 
							typeof(TSubscription).GetEventStoreName(), 
							handleResolvedEvent, 
							getCheckpoint, 
							getEventHandlingQueueKey ?? (resolvedEvent => string.Empty))
					
				}));
		}

		public EventBus2 RegisterVolatileSubscriber<TSubscriber>(TSubscriber subscriber) where TSubscriber : IMessageHandler
		{
			return RegisterVolatileSubscriber<TSubscriber, TSubscriber>(subscriber);
		}

		public EventBus2 RegisterVolatileSubscriber<TSubscription, TSubscriber>(TSubscriber subscriber) where TSubscription : IMessageHandler where TSubscriber : TSubscription
		{
			return RegisterVolatileSubscriber<TSubscription>
			(
				SubscriberResolvedEventHandleFactory
					.CreateSubscriberResolvedEventHandle<TSubscriber, Task>(delegate { return Task.CompletedTask; })
					.Partial(subscriber)
			);
		}

		public EventBus2 RegisterVolatileSubscriber<TSubscription>(Func<ResolvedEvent, Task> handleResolvedEvent) where TSubscription : IMessageHandler
		{
			return new EventBus2(_createConnection,
				_subscriberStarters.Concat(new Func<Task<Subscriber>>[]
				{
					() => 
						Subscriber.StartVolatileSubscriber(
							_createConnection,
							typeof(TSubscription).GetEventStoreName(),
							handleResolvedEvent)
				}));
		}

		public EventBus2 RegisterPersistentSubscriber<TSubscriber>(TSubscriber subscriber) where TSubscriber : IMessageHandler
		{
			return RegisterPersistentSubscriber<TSubscriber, TSubscriber>(subscriber);
		}

		public EventBus2 RegisterPersistentSubscriber<TSubscription, TSubscriber>(TSubscriber subscriber) where TSubscription : IMessageHandler where TSubscriber : TSubscription
		{
			return RegisterPersistentSubscriber<TSubscription, TSubscriber>
			(
				SubscriberResolvedEventHandleFactory
					.CreateSubscriberResolvedEventHandle<TSubscriber, Task>(delegate { return Task.CompletedTask; })
					.Partial(subscriber)
			);
		}

		public EventBus2 RegisterPersistentSubscriber<TSubscription, TSubscriber>(Func<ResolvedEvent, Task> handleResolvedEvent) where TSubscription : IMessageHandler
		{
			return new EventBus2(_createConnection,
				_subscriberStarters.Concat(new Func<Task<Subscriber>>[]
				{
					() => 
						Subscriber.StartPersistentSubscriber(
							_createConnection, 
							typeof(TSubscription).GetEventStoreName(), 
							typeof(TSubscriber).GetEventStoreName(), 
							handleResolvedEvent)
				}));
		}

		public async Task<EventBusHandle2> Start()
		{
			var subscribers = await Task.WhenAll(_subscriberStarters.Select(x => x()));
			return new EventBusHandle2(() => Parallel.ForEach(subscribers, x => x.Stop()));
		}
	}
}
