using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using EventStore.ClientAPI;

namespace infra
{
    public class CatchUpSubscription : ISubscription
    {
        private readonly IEventStoreConnectionFactory _eventStoreConnectionFactory;
        private readonly ILogger _logger;
        private readonly string _streamName;
        private readonly int _reconnectDelayInMilliseconds;
        private readonly Func<ResolvedEvent, Task<bool>> _tryHandleEvent;
        private readonly Func<Task<int?>> _getLastCheckpoint;

        public CatchUpSubscription(IEventStoreConnectionFactory eventStoreConnectionFactory,
            string streamName,
            Func<ResolvedEvent, Task<bool>> tryHandleEvent,
            int reconnectDelayInMilliseconds,
            Func<Task<int?>> getLastCheckpoint,
            ILogger logger)
        {
            _eventStoreConnectionFactory = eventStoreConnectionFactory;
            _streamName = streamName;
            _tryHandleEvent = tryHandleEvent;
            _reconnectDelayInMilliseconds = reconnectDelayInMilliseconds;
            _getLastCheckpoint = getLastCheckpoint;
            _logger = logger;
        }

        public async Task StartAsync()
        {
            while (true)
            {
                var connection = _eventStoreConnectionFactory.CreateConnection();
                await connection.ConnectAsync();
                var lastCheckpoint = await _getLastCheckpoint();
                try
                {
                    connection.SubscribeToStreamFrom(_streamName, lastCheckpoint, CatchUpSubscriptionSettings.Default, OnEventReceived, subscriptionDropped: OnSubscriptionDropped(connection));
                    return;
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Exception occurred attempting to connect to catchup subscription. Subscriber Info: StreamName:{0}", _streamName);
                }
                connection.Dispose();
                await Task.Delay(_reconnectDelayInMilliseconds);
            }
        }

        private async void OnEventReceived(EventStoreCatchUpSubscription subscription, ResolvedEvent resolvedEvent)
        {
            try
            {
                await _tryHandleEvent(resolvedEvent);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "CatchUpSubscription failed to handle {@Event} from stream {OriginalStreamId}", resolvedEvent.Event, resolvedEvent.OriginalStreamId);
            }
        }

        private Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> OnSubscriptionDropped(IDisposable connection)
        {
            return async (subscription, reason, exception) =>
            {
                connection.Dispose();
                _logger.Error(exception, "CatchUpSubscription was dropped. Reason:{0}. Subscriber Info: StreamName:{1}", reason, _streamName);
                await StartAsync();
            };
        }

    }

}
