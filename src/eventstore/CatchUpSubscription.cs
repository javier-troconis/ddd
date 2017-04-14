using System;
using System.Threading.Tasks;

using EventStore.ClientAPI;

namespace eventstore
{
	public sealed class CatchUpSubscription
	{
		private readonly Func<IEventStoreConnection> _createConnection;
		private readonly string _streamName;
		private readonly TimeSpan _reconnectDelay;
		private readonly Func<ResolvedEvent, Task> _handleEvent;
		private readonly Func<Task<long?>> _getCheckpoint;

		public CatchUpSubscription(
			Func<IEventStoreConnection> createConnection,
			string streamName,
			Func<ResolvedEvent, Task> handleEvent,
			TimeSpan reconnectDelay,
			Func<Task<long?>> getCheckpoint)
		{
			_createConnection = createConnection;
			_streamName = streamName;
			_handleEvent = handleEvent;
			_reconnectDelay = reconnectDelay;
			_getCheckpoint = getCheckpoint;
		}

		public async Task Start()
		{
			while (true)
			{
				var connection = _createConnection();
				try
				{
					var checkpoint = await _getCheckpoint();
					await connection.ConnectAsync();
					connection.SubscribeToStreamFrom(_streamName, checkpoint, CatchUpSubscriptionSettings.Default, OnEventAppeared, subscriptionDropped: OnSubscriptionDropped(connection));
					return;
				}
				catch
				{
					connection.Dispose();
				}
				await Task.Delay(_reconnectDelay);
			}
		}

		private void OnEventAppeared(EventStoreCatchUpSubscription subscription, ResolvedEvent resolvedEvent)
		{
			_handleEvent(resolvedEvent);
		}

		private Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> OnSubscriptionDropped(IDisposable connection)
		{
			return async (subscription, reason, exception) =>
			{
				connection.Dispose();
				await Start();
			};
		}

	}

}
