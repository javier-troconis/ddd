using System;
using System.Threading.Tasks;

using EventStore.ClientAPI;

namespace eventstore
{
	public sealed class VolatileSubscriber
	{
		private readonly Lazy<Task<IEventStoreConnection>> _connection;
		private readonly string _streamName;
		private readonly TimeSpan _reconnectDelay;
		private readonly Action<EventStoreSubscription, ResolvedEvent> _handleEvent;

		public VolatileSubscriber(
			Func<IEventStoreConnection> createConnection,
			string streamName,
			Action<EventStoreSubscription, ResolvedEvent> handleEvent,
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
					await connection.SubscribeToStreamAsync(_streamName, true, _handleEvent, OnSubscriptionDropped);
					break;
				}
				catch
				{
					await Task.Delay(_reconnectDelay);
				}
			}
		}

		private async void OnSubscriptionDropped(EventStoreSubscription subscription, SubscriptionDropReason reason, Exception exception)
		{
			if (reason == SubscriptionDropReason.UserInitiated)
			{
				return;
			}
			await Start();
		}

	}

}
