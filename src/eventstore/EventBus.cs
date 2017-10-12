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
	public sealed class EventBusRegistry
	{
		internal readonly IEnumerable<Func<Func<IEventStoreConnection>, Task<Subscriber>>> _startSubscribers;

		private EventBusRegistry(IEnumerable<Func<Func<IEventStoreConnection>, Task<Subscriber>>> startSubscribers)
		{
			_startSubscribers = startSubscribers;
		}

		public EventBusRegistry RegisterCatchupSubscriber<TSubscriber>(TSubscriber subscriber, Func<Task<long?>> getCheckpoint, Func<ResolvedEvent, string> getEventHandlingQueueKey = null) where TSubscriber : IMessageHandler
		{
			return RegisterCatchupSubscriber<TSubscriber, TSubscriber>(subscriber, getCheckpoint, getEventHandlingQueueKey);
		}

		public EventBusRegistry RegisterCatchupSubscriber<TSubscription, TSubscriber>(TSubscriber subscriber, Func<Task<long?>> getCheckpoint, Func<ResolvedEvent, string> getEventHandlingQueueKey = null) where TSubscription : IMessageHandler where TSubscriber : TSubscription
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

		public EventBusRegistry RegisterCatchupSubscriber<TSubscription>(Func<ResolvedEvent, Task> handleResolvedEvent, Func<Task<long?>> getCheckpoint, Func<ResolvedEvent, string> getEventHandlingQueueKey = null) where TSubscription : IMessageHandler
		{
			return new EventBusRegistry(
				_startSubscribers.Concat(new Func<Func<IEventStoreConnection>, Task<Subscriber>>[]
				{
					createConnection =>
						Subscriber.StartCatchUpSubscriber(
							createConnection,
							typeof(TSubscription).GetEventStoreName(),
							handleResolvedEvent,
							getCheckpoint,
							getEventHandlingQueueKey ?? (resolvedEvent => string.Empty))

				}));
		}

		public EventBusRegistry RegisterVolatileSubscriber<TSubscriber>(TSubscriber subscriber) where TSubscriber : IMessageHandler
		{
			return RegisterVolatileSubscriber<TSubscriber, TSubscriber>(subscriber);
		}

		public EventBusRegistry RegisterVolatileSubscriber<TSubscription, TSubscriber>(TSubscriber subscriber) where TSubscription : IMessageHandler where TSubscriber : TSubscription
		{
			return RegisterVolatileSubscriber<TSubscription>
			(
				SubscriberResolvedEventHandleFactory
					.CreateSubscriberResolvedEventHandle<TSubscriber, Task>(delegate { return Task.CompletedTask; })
					.Partial(subscriber)
			);
		}

		public EventBusRegistry RegisterVolatileSubscriber<TSubscription>(Func<ResolvedEvent, Task> handleResolvedEvent) where TSubscription : IMessageHandler
		{
			return new EventBusRegistry(
				_startSubscribers.Concat(new Func<Func<IEventStoreConnection>, Task<Subscriber>>[]
				{
					createConnection =>
						Subscriber.StartVolatileSubscriber(
							createConnection,
							typeof(TSubscription).GetEventStoreName(),
							handleResolvedEvent)
				}));
		}

		public EventBusRegistry RegisterPersistentSubscriber<TSubscriber>(TSubscriber subscriber) where TSubscriber : IMessageHandler
		{
			return RegisterPersistentSubscriber<TSubscriber, TSubscriber>(subscriber);
		}

		public EventBusRegistry RegisterPersistentSubscriber<TSubscription, TSubscriber>(TSubscriber subscriber) where TSubscription : IMessageHandler where TSubscriber : TSubscription
		{
			return RegisterPersistentSubscriber<TSubscription, TSubscriber>
			(
				SubscriberResolvedEventHandleFactory
					.CreateSubscriberResolvedEventHandle<TSubscriber, Task>(delegate { return Task.CompletedTask; })
					.Partial(subscriber)
			);
		}

		public EventBusRegistry RegisterPersistentSubscriber<TSubscription, TSubscriber>(Func<ResolvedEvent, Task> handleResolvedEvent) where TSubscription : IMessageHandler
		{
			return new EventBusRegistry(
				_startSubscribers.Concat(new Func<Func<IEventStoreConnection>, Task<Subscriber>>[]
				{
					createConnection =>
						Subscriber.StartPersistentSubscriber(
							createConnection,
							typeof(TSubscription).GetEventStoreName(),
							typeof(TSubscriber).GetEventStoreName(),
							handleResolvedEvent)
				}));
		}

		public static EventBusRegistry Create()
		{
			return new EventBusRegistry(Enumerable.Empty<Func<Func<IEventStoreConnection>, Task<Subscriber>>>());
		}
	}

	public sealed class EventBus
	{
		public readonly Action Stop;

		public EventBus(Action stop)
		{
			Stop = stop;
		}

		public static async Task<EventBus> Start(Func<IEventStoreConnection> createConnection, Func<EventBusRegistry, EventBusRegistry> registerSubscribers)
		{
			var startSubscribers = registerSubscribers(EventBusRegistry.Create())._startSubscribers;
			var subscribers = await Task.WhenAll(startSubscribers.Select(startSubscriber => startSubscriber(createConnection)));
			return new EventBus(() => Parallel.ForEach(subscribers, subscriber => subscriber.Stop()));
			//var subscribers = await Task.WhenAll(_startSubscribers.Select(x => x()));
			//return new EventBusHandle2(() => Parallel.ForEach(subscribers, x => x.Stop()));
		}
	}

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
		private readonly IEnumerable<Func<Task<Subscriber>>> _startSubscribers;
		private readonly Func<IEventStoreConnection> _createConnection;

		public EventBus2(Func<IEventStoreConnection> createConnection) : this(createConnection, Enumerable.Empty<Func<Task<Subscriber>>>())
		{

		}

		private EventBus2(Func<IEventStoreConnection> createConnection, IEnumerable<Func<Task<Subscriber>>> startSubscribers)
		{
			_createConnection = createConnection;
			_startSubscribers = startSubscribers;
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
				_startSubscribers.Concat(new Func<Task<Subscriber>>[]
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
				_startSubscribers.Concat(new Func<Task<Subscriber>>[]
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
				_startSubscribers.Concat(new Func<Task<Subscriber>>[]
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
			var subscribers = await Task.WhenAll(_startSubscribers.Select(x => x()));
			return new EventBusHandle2(() => Parallel.ForEach(subscribers, x => x.Stop()));
		}
	}
}
