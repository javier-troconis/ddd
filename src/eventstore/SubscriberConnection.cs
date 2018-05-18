using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using shared;

namespace eventstore
{
  
    public struct SubscriberConnection
    {
	    public readonly Action Disconnect;
	    
		private SubscriberConnection(Action disconnect)
		{
			Disconnect = disconnect;
		}

	    public static async Task<SubscriberConnection> ConnectCatchUpSubscriber(
			Func<IEventStoreConnection> createConnection,
		    string streamName,
		    Func<ResolvedEvent, Task> handleEvent,
		    Func<Task<long?>> getCheckpoint,
		    Func<ResolvedEvent, string> getEventHandlingQueueKey,
            Action<SubscriptionDropReason, Exception> subscriptionDropped = null)
	    {
			var queue = new TaskQueue();
		    var connection = createConnection();
		    var checkpoint = await getCheckpoint();
		    await connection.ConnectAsync();
			var s = connection.SubscribeToStreamFrom(
				streamName, 
				checkpoint, 
				CatchUpSubscriptionSettings.Default,
				(subscription, resolvedEvent) =>
				{
					var channelName = getEventHandlingQueueKey(resolvedEvent);
					return queue.SendToChannel
					(
                        channelName,
						() => handleEvent(resolvedEvent)
					);
				}, 
				subscriptionDropped: (subscription, dropReason, exception) =>
				{
					connection.Close();
                    subscriptionDropped?.Invoke(dropReason, exception);
                });
		    return new SubscriberConnection(s.Stop);
		}

	    public static async Task<SubscriberConnection> ConnectVolatileSubscriber(
			Func<IEventStoreConnection> createConnection,
		    string streamName,
		    Func<ResolvedEvent, Task> handleEvent,
            Action<SubscriptionDropReason, Exception> subscriptionDropped = null)
	    {
		    var connection = createConnection();
		    await connection.ConnectAsync();
			var s = await connection.SubscribeToStreamAsync(
				streamName, 
				true,
				(subscription, resolvedEvent) => handleEvent(resolvedEvent), 
				subscriptionDropped: (subscription, dropReason, exception) =>
				{
					connection.Close();
                    subscriptionDropped?.Invoke(dropReason, exception);
                });
			return new SubscriberConnection(s.Close);
		}

	    public static async Task<SubscriberConnection> ConnectPersistentSubscriber(
			Func<IEventStoreConnection> createConnection,
		    string streamName,
		    string groupName,
		    Func<ResolvedEvent, Task> handleEvent,
            Action<SubscriptionDropReason, Exception> subscriptionDropped = null)
	    {
			var connection = createConnection();

			await connection.ConnectAsync();

            // workaround due to a bug in the eventstore -> https://eventstore.freshdesk.com/support/tickets/831
            var isSubscriptionDroppedByUser = false;
            //
			    
			var s = await connection.ConnectToPersistentSubscriptionAsync(
				streamName,
				groupName,
				async (subscription, resolvedEvent) =>
				{
					try
					{
						await handleEvent(resolvedEvent);
						subscription.Acknowledge(resolvedEvent);
					}
					catch (Exception ex)
					{
						subscription.Fail(resolvedEvent, PersistentSubscriptionNakEventAction.Unknown, ex.Message);
					}
				},
				subscriptionDropped: (subscription, dropReason, exception) =>
				{
                    // workaround due to a bug in the eventstore -> https://eventstore.freshdesk.com/support/tickets/831
                    if (!isSubscriptionDroppedByUser && dropReason == SubscriptionDropReason.UserInitiated)
                    {
                        dropReason = SubscriptionDropReason.ConnectionClosed;
                    }
                    //
					connection.Close();
                    subscriptionDropped?.Invoke(dropReason, exception);
                },
				autoAck: false);
			return new SubscriberConnection(
				() =>
				{
					isSubscriptionDroppedByUser = true;
					s.Stop(TimeSpan.FromSeconds(15));
				});
	    }


	}
    
}
