using System;
using System.Threading.Tasks;

using EventStore.ClientAPI;

namespace eventstore
{
	public sealed class CatchUpSubscriber
	{
		private readonly Lazy<Task<IEventStoreConnection>> _connection;
		private readonly string _streamName;
		private readonly TimeSpan _reconnectDelay;
		private readonly Action<EventStoreCatchUpSubscription, ResolvedEvent> _handleEvent;
		private readonly Func<Task<long?>> _getCheckpoint;

		public CatchUpSubscriber(
			Func<IEventStoreConnection> createConnection,
			string streamName,
			Action<EventStoreCatchUpSubscription, ResolvedEvent> handleEvent,
			TimeSpan reconnectDelay,
			Func<Task<long?>> getCheckpoint)
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
			_reconnectDelay = reconnectDelay;
			_getCheckpoint = getCheckpoint;
		}

		public async Task Start()
		{
			while (true)
			{
				try
				{
					var connection = await _connection.Value;
					var checkpoint = await _getCheckpoint();
					connection.SubscribeToStreamFrom(_streamName, checkpoint, CatchUpSubscriptionSettings.Default, _handleEvent, subscriptionDropped: OnSubscriptionDropped);
					break;
				}
				catch
				{
					await Task.Delay(_reconnectDelay);
				}
			}
		}

		private async void OnSubscriptionDropped(EventStoreCatchUpSubscription subscription, SubscriptionDropReason reason, Exception exception)
		{
			await Start();
		}

	}

}
