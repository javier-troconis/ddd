using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using EventStore.ClientAPI;

namespace infra
{
    public class CatchUpSubscription : ISubscription
    {
        private readonly Func<IEventStoreConnection> _createConnection;
        private readonly string _streamName;
        private readonly int _reconnectDelayInMilliseconds;
        private readonly Func<ResolvedEvent, Task> _handleEvent;
        private readonly Func<Task<int?>> _getLastCheckpoint;

        public CatchUpSubscription(
			Func<IEventStoreConnection> createConnection,
            string streamName,
            Func<ResolvedEvent, Task> handleEvent,
            int reconnectDelayInMilliseconds,
            Func<Task<int?>> getLastCheckpoint)
        {
			_createConnection = createConnection;
            _streamName = streamName;
            _handleEvent = handleEvent;
            _reconnectDelayInMilliseconds = reconnectDelayInMilliseconds;
            _getLastCheckpoint = getLastCheckpoint;
        }

        public async Task StartAsync()
        {
            while (true)
            {
                var connection = _createConnection();
                await connection.ConnectAsync();
                var lastCheckpoint = await _getLastCheckpoint();
                try
                {
                    connection.SubscribeToStreamFrom(_streamName, lastCheckpoint, CatchUpSubscriptionSettings.Default, OnEventReceived, subscriptionDropped: OnSubscriptionDropped(connection));
                    return;
                }
                catch
                {
                    
                }
                connection.Dispose();
                await Task.Delay(_reconnectDelayInMilliseconds);
            }
        }

        private void OnEventReceived(EventStoreCatchUpSubscription subscription, ResolvedEvent resolvedEvent)
        {
            _handleEvent(resolvedEvent);
        }

        private Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> OnSubscriptionDropped(IDisposable connection)
        {
            return async (subscription, reason, exception) =>
            {
				connection.Dispose();
                await StartAsync();
            };
        }

    }

}
