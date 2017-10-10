using System;
using System.Threading.Tasks;

using EventStore.ClientAPI;
using shared;

namespace eventstore
{
	public sealed class CatchUpSubscriber
	{
		private readonly TaskQueue _queue = new TaskQueue();
		private readonly Lazy<Task<IEventStoreConnection>> _connection;
		private readonly string _streamName;
		private readonly Func<ResolvedEvent, Task> _handleEvent;
		private readonly Func<Task<long?>> _getCheckpoint;
		private readonly Func<ResolvedEvent, string> _getEventHandlingQueueKey;
		private readonly TimeSpan _reconnectDelay;
		
		public CatchUpSubscriber(
			Func<IEventStoreConnection> createConnection,
			string streamName,
			Func<ResolvedEvent, Task> handleEvent,
			Func<Task<long?>> getCheckpoint,
			Func<ResolvedEvent, string> getEventHandlingQueueKey,
			TimeSpan reconnectDelay
			)
		{
			_connection = new Lazy<Task<IEventStoreConnection>>(
				async () =>
				{
					var connection = createConnection();
					await connection.ConnectAsync();
					return connection;
				});
			_streamName = streamName;
			_handleEvent = handleEvent;
			_getCheckpoint = getCheckpoint;
			_getEventHandlingQueueKey = getEventHandlingQueueKey;
			_reconnectDelay = reconnectDelay;
		}

		public async Task Start()
		{
			while (true)
			{
				try
				{
					var connection = await _connection.Value;
					var checkpoint = await _getCheckpoint();
					connection.SubscribeToStreamFrom(_streamName, checkpoint, CatchUpSubscriptionSettings.Default, OnEventAppeared, subscriptionDropped: OnSubscriptionDropped);
					break;
				}
				catch
				{
					await Task.Delay(_reconnectDelay);
				}
			}
		}

		private Task OnEventAppeared(EventStoreCatchUpSubscription subscription, ResolvedEvent resolvedEvent)
		{
			return _queue.SendToChannel
				(
					_getEventHandlingQueueKey(resolvedEvent),
					() => _handleEvent(resolvedEvent)
				);
		}

		private void OnSubscriptionDropped(EventStoreCatchUpSubscription subscription, SubscriptionDropReason reason, Exception exception)
		{
			
		}

	}

}
