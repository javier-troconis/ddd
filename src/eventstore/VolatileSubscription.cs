using System;
using System.Threading.Tasks;

using EventStore.ClientAPI;

namespace eventstore
{
	public sealed class VolatileSubscription
	{
		private readonly Func<IEventStoreConnection> _createConnection;
		private readonly string _streamName;
		private readonly TimeSpan _reconnectDelay;
		private readonly Func<ResolvedEvent, Task> _handleResolvedEvent;

		public VolatileSubscription(
			Func<IEventStoreConnection> createConnection,
			string streamName,
			Func<ResolvedEvent, Task> handleResolvedEvent,
			TimeSpan reconnectDelay)
		{
			_createConnection = createConnection;
			_streamName = streamName;
			_handleResolvedEvent = handleResolvedEvent;
			_reconnectDelay = reconnectDelay;
		}

		public async Task Start()
		{
			while (true)
			{
				var connection = _createConnection();
				try
				{
					await connection.ConnectAsync();
					await connection.SubscribeToStreamAsync(_streamName, true, OnEventAppeared, OnSubscriptionDropped(connection));
					return;
				}
				catch
				{
					connection.Dispose();
				}
				await Task.Delay(_reconnectDelay);
			}
		}

		private void OnEventAppeared(EventStoreSubscription subscription, ResolvedEvent resolvedEvent)
		{
			_handleResolvedEvent(resolvedEvent);
		}

		private Action<EventStoreSubscription, SubscriptionDropReason, Exception> OnSubscriptionDropped(IDisposable connection)
		{
			return async (subscription, reason, exception) =>
			{
				connection.Dispose();
				await Start();
			};
		}

	}

}
