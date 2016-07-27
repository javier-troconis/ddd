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
        private readonly IEventStoreConnection _eventStoreConnection;
        private readonly string _consumerGroupName;
        private readonly string _streamName;
        private readonly Func<ResolvedEvent, Task<bool>> _tryHandleEvent;
        private readonly int _reconnectDelayInMilliseconds;
        private readonly ILogger _logger;


        public PersistentSubscription(IEventStoreConnection eventStoreConnection,
            string streamName,
            string consumerGroupName,
            Func<ResolvedEvent, Task<bool>> tryHandleEvent,
            int reconnectDelayInMilliseconds,
            ILogger logger)
        {
            _eventStoreConnection = eventStoreConnection;
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
                try
                {
                    await _eventStoreConnection.ConnectToPersistentSubscriptionAsync(_streamName, _consumerGroupName, OnEventReceived, OnSubscriptionDropped, autoAck: false);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Exception occurred attempting to reconnect to dropped persistent subscription. Subscriber Info: StreamName:{0}, GroupName:{1}", _streamName, _consumerGroupName);
                }

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

        private async void OnSubscriptionDropped(EventStorePersistentSubscriptionBase subscription, SubscriptionDropReason reason, Exception exception)
        {
            _logger.Error(exception, "PersistentSubscription was dropped. Reason:{0}. Subscriber Info: StreamName:{1}, GroupName:{2}", reason, _streamName, _consumerGroupName);
            await StartAsync();
        }
    }
}
