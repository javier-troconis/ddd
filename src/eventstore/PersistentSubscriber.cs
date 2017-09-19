using System;
using System.Threading.Tasks;

using EventStore.ClientAPI;

namespace eventstore
{
	public sealed class PersistentSubscriber
	{
		private readonly string _groupName;
		private readonly Lazy<Task<IEventStoreConnection>> _connection;
		private readonly string _streamName;
		private readonly Action<EventStorePersistentSubscriptionBase, ResolvedEvent> _handleEvent;
		private readonly TimeSpan _reconnectDelay;

		public PersistentSubscriber(
			Func<IEventStoreConnection> createConnection,
			string streamName,
			string groupName,
			Action<EventStorePersistentSubscriptionBase, ResolvedEvent> handleEvent,
			TimeSpan reconnectDelay)
		{
			_connection = new Lazy<Task<IEventStoreConnection>>(
				async () =>
				{
					var connection = createConnection();
					await connection.ConnectAsync();
					return connection;
				});
			_streamName = streamName;
			_groupName = groupName;
			_handleEvent = handleEvent;
			_reconnectDelay = reconnectDelay;
		}

		public async Task Start()
		{
			while (true)
			{
				try
				{
					var connection = await _connection.Value;
					await connection.ConnectToPersistentSubscriptionAsync(_streamName, _groupName, _handleEvent, OnSubscriptionDropped, autoAck: false);
					break;
				}
				catch
				{
					await Task.Delay(_reconnectDelay);
				}
			}
		}

		private async void OnSubscriptionDropped(EventStorePersistentSubscriptionBase subscription, SubscriptionDropReason reason, Exception exception)
		{
			if (reason == SubscriptionDropReason.UserInitiated)
			{
				return;
			}
			await Start();
		}
	}
}
