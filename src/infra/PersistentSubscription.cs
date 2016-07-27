using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using EventStore.ClientAPI;

namespace infra
{
    public class PersistentSubscription : ISubscription
    {
        private readonly string _consumerGroupName;
        private readonly EventStoreConnectionFactory _eventStoreConnectionFactory;
        private readonly string _streamName;
        private readonly Func<ResolvedEvent, Task<bool>> _tryHandleEvent;
        private readonly int _reconnectDelayInMilliseconds;
        private readonly ILogger _logger;


        public PersistentSubscription(EventStoreConnectionFactory eventStoreConnectionFactory,
            string streamName,
            string consumerGroupName,
            Func<ResolvedEvent, Task<bool>> tryHandleEvent,
            int reconnectDelayInMilliseconds,
            ILogger logger)
        {
            _eventStoreConnectionFactory = eventStoreConnectionFactory;
            _streamName = streamName;
            _consumerGroupName = consumerGroupName;
            _tryHandleEvent = tryHandleEvent;
            _reconnectDelayInMilliseconds = reconnectDelayInMilliseconds;
            _logger = logger;
        }

      

        public async Task StartAsync()
        {
            while (true)
            {
                var connection = _eventStoreConnectionFactory.Create(x => x.KeepReconnecting());
                try
                {
                    await connection.ConnectAsync();
                    await connection.ConnectToPersistentSubscriptionAsync(_streamName, _consumerGroupName, OnEventReceived, OnSubscriptionDropped(connection), autoAck: false);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Exception occurred attempting to connect to persistent subscription. Subscriber Info: StreamName:{0}, GroupName:{1}", _streamName, _consumerGroupName);
                }
                connection.Dispose();
                await Task.Delay(_reconnectDelayInMilliseconds);
            }
        }

        private async void OnEventReceived(EventStorePersistentSubscriptionBase subscription, ResolvedEvent resolvedEvent)
        {
            try
            {
                var isEventHandled = await _tryHandleEvent(resolvedEvent);
                if (isEventHandled)
                {
                    subscription.Acknowledge(resolvedEvent);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "PersistentSubscription failed to handle {@Event} from stream {OriginalStreamId}", resolvedEvent.Event, resolvedEvent.OriginalStreamId);
                subscription.Fail(resolvedEvent, PersistentSubscriptionNakEventAction.Unknown, ex.Message);
            }
        }

        private Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> OnSubscriptionDropped(IEventStoreConnection connection)
        {
            return async (subscription, reason, exception) =>
            {
                connection.Dispose();
                _logger.Error(exception, "PersistentSubscription was dropped. Reason:{0}. Subscriber Info: StreamName:{1}, GroupName:{2}", reason, _streamName, _consumerGroupName);
                await StartAsync();
            };
        }
    }
}
