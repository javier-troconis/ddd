using System;
using System.Threading.Tasks;

using EventStore.ClientAPI;

namespace eventstore
{
	public sealed class PersistentSubscription
	{
		private readonly string _groupName;
		private readonly Lazy<Task<IEventStoreConnection>> _connection;
		private readonly string _streamName;
		private readonly Func<ResolvedEvent, Task> _handleEvent;
		private readonly TimeSpan _reconnectDelay;

		public PersistentSubscription(
			Func<IEventStoreConnection> createConnection,
			string streamName,
			string groupName,
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
					await connection.ConnectToPersistentSubscriptionAsync(_streamName, _groupName, OnEventAppeared, OnSubscriptionDropped, autoAck: false);
					break;
				}
				catch
				{
					await Task.Delay(_reconnectDelay);
				}
			}
		}

		private async void OnEventAppeared(EventStorePersistentSubscriptionBase subscription, ResolvedEvent resolvedEvent)
		{
			try
			{
				await _handleEvent(resolvedEvent);
				subscription.Acknowledge(resolvedEvent);
			}
			catch (Exception ex)
			{
				subscription.Fail(resolvedEvent, PersistentSubscriptionNakEventAction.Unknown, ex.Message);
			}
			
		}

		private async void OnSubscriptionDropped(EventStorePersistentSubscriptionBase subscription, SubscriptionDropReason reason, Exception exception)
		{
			await Start();
		}
	}
}
