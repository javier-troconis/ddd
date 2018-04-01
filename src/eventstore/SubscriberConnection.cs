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
                    subscriptionDropped?.Invoke(dropReason, exception);
                });
		    return new SubscriberConnection(
			    () =>
			    {
				    s.Stop();
					connection.Close();
			    });
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
                    subscriptionDropped?.Invoke(dropReason, exception);
                });
			return new SubscriberConnection(
				() =>
				{
					s.Close();
					connection.Close();
				});
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

            while (true)
		    {
                // workaround due to a bug in the eventstore -> https://eventstore.freshdesk.com/support/tickets/831
                var isCloseConnectionCalledByUser = false;
                //
			    try
			    {
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
                            if (!isCloseConnectionCalledByUser && dropReason == SubscriptionDropReason.UserInitiated)
                            {
                                dropReason = SubscriptionDropReason.ConnectionClosed;
                            }
                            //
                            subscriptionDropped?.Invoke(dropReason, exception);
                        },
					    autoAck: false);
				    return new SubscriberConnection(
					    () =>
					    {
						    s.Stop(TimeSpan.FromSeconds(15));
						    connection.Close();
					    });
			    }
			    catch
			    {
				    await Task.Delay(TimeSpan.FromSeconds(1));
			    }
		    }
	    }


	}
    
}
