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
		private readonly Func<ResolvedEvent, Task> _handleResolvedEvent;
		private readonly Func<Task<long?>> _getCheckpoint;

		public CatchUpSubscription(
			Func<IEventStoreConnection> createConnection,
			string streamName,
			Func<ResolvedEvent, Task> handleResolvedEvent,
			TimeSpan reconnectDelay,
			Func<Task<long?>> getCheckpoint)
		{
			_createConnection = createConnection;
			_streamName = streamName;
			_handleResolvedEvent = handleResolvedEvent;
			_reconnectDelay = reconnectDelay;
			_getCheckpoint = getCheckpoint;
		}

		public async Task Start()
		{
			while (true)
			{
				var connection = _createConnection();
				var checkpoint = await _getCheckpoint();
				try
				{
					await connection.ConnectAsync();
					connection.SubscribeToStreamFrom(_streamName, checkpoint, CatchUpSubscriptionSettings.Default, OnEventReceived, subscriptionDropped: OnSubscriptionDropped(connection));
					return;
				}
				catch
				{
					connection.Dispose();
				}
				await Task.Delay(_reconnectDelay);
			}
		}

		private void OnEventReceived(EventStoreCatchUpSubscription subscription, ResolvedEvent resolvedEvent)
		{
			_handleResolvedEvent(resolvedEvent);
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
