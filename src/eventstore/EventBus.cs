using System;
using System.Collections.Generic;
using System.Linq;
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
		private readonly IEnumerable<Func<Task>> _subscriptions;
		private readonly Func<IEventStoreConnection> _createConnection;

		public EventBus(Func<IEventStoreConnection> createConnection)
			: this(createConnection, Enumerable.Empty<Func<Task>>())
		{
		}

		private EventBus(Func<IEventStoreConnection> createConnection, IEnumerable<Func<Task>> subscriptions)
		{
			_createConnection = createConnection;
			_subscriptions = subscriptions;
		}

        public EventBus RegisterCatchupSubscriber<TSubscriber>(IMessageHandler<ResolvedEvent, Task<ResolvedEvent>> resolvedEventMessageHandler, Func<Task<long?>> getCheckpoint) where TSubscriber : IMessageHandler
        {
            return RegisterCatchupSubscriber(typeof(TSubscriber).GetEventStoreName(), resolvedEventMessageHandler.Handle, getCheckpoint);
        }

        public EventBus RegisterCatchupSubscriber(string streamName, Func<ResolvedEvent, Task<ResolvedEvent>> handleResolvedEvent, Func<Task<long?>> getCheckpoint)
		{
			return new EventBus(_createConnection,
				_subscriptions.Concat(new Func<Task>[]
				{
					new CatchUpSubscription(
						_createConnection,
						streamName,
						handleResolvedEvent,
						TimeSpan.FromSeconds(1),
						getCheckpoint).Start
				}));
		}

		public EventBus RegisterVolatileSubscriber(string streamName, Func<ResolvedEvent, Task<ResolvedEvent>> handleResolvedEvent)
		{
			return new EventBus(_createConnection,
				_subscriptions.Concat(new Func<Task>[]
				{
					new VolatileSubscription(
						_createConnection,
						streamName,
						handleResolvedEvent,
						TimeSpan.FromSeconds(1)).Start
				}));
		}

		public EventBus RegisterPersistentSubscriber(string streamName, string groupName, Func<ResolvedEvent, Task<ResolvedEvent>> handleResolvedEvent)
		{
			return new EventBus(_createConnection,
				_subscriptions.Concat(new Func<Task>[]
				{
					new PersistentSubscription(
						_createConnection,
						streamName,
						groupName,
						handleResolvedEvent,
						TimeSpan.FromSeconds(1)).Start
				}));
		}

		public void Start()
		{
            Parallel.ForEach(_subscriptions, start => start());
		}
	}
}
