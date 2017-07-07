using System;
using System.Threading.Tasks;

using EventStore.ClientAPI;

namespace eventstore
{
	public sealed class VolatileSubscription
	{
		private readonly Lazy<Task<IEventStoreConnection>> _connection;
		private readonly string _streamName;
		private readonly TimeSpan _reconnectDelay;
		private readonly Func<ResolvedEvent, Task> _handleEvent;

		public VolatileSubscription(
			Func<IEventStoreConnection> createConnection,
			string streamName,
			Func<ResolvedEvent, Task> handleEvent,
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
					await connection.SubscribeToStreamAsync(_streamName, true, OnEventAppeared, OnSubscriptionDropped);
					break;
				}
				catch
				{
					await Task.Delay(_reconnectDelay);
				}
			}
		}

		private void OnEventAppeared(EventStoreSubscription subscription, ResolvedEvent resolvedEvent)
		{
			_handleEvent(resolvedEvent);
		}

		private async void OnSubscriptionDropped(EventStoreSubscription subscription, SubscriptionDropReason reason, Exception exception)
		{
			await Start();
		}

	}

}
