using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using shared;

namespace eventstore
{
	public class SubscriberHandle
	{
		public readonly Action Stop;

		internal SubscriberHandle(Action stop)
		{
			Stop = stop;
		}
	}
    public static class Subscriber
    {
	    public static async Task<SubscriberHandle> StartCatchUpSubscriber(
			Func<IEventStoreConnection> createConnection,
		    string streamName,
		    Func<ResolvedEvent, Task> handleEvent,
		    Func<Task<long?>> getCheckpoint,
		    Func<ResolvedEvent, string> getEventHandlingQueueKey)
	    {
			var queue = new TaskQueue();
			var connection = createConnection();
		    var checkpoint = await getCheckpoint();
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
					
				});
		    return new SubscriberHandle(s.Stop);
		}

	    public static async Task<SubscriberHandle> StartVolatileSubscriber(
			Func<IEventStoreConnection> createConnection,
		    string streamName,
		    Func<ResolvedEvent, Task> handleEvent)
	    {
		    var connection = createConnection();
			var s = await connection.SubscribeToStreamAsync(
				streamName, 
				true,
				(subscription, resolvedEvent) => handleEvent(resolvedEvent), 
				subscriptionDropped: (subscription, dropReason, exception) =>
				{
					
				});
			return new SubscriberHandle(s.Close);
		}

	    public static async Task<SubscriberHandle> StartPersistentSubscriber(
			Func<IEventStoreConnection> createConnection,
		    string streamName,
		    string groupName,
		    Func<ResolvedEvent, Task> handleEvent)
	    {
			var connection = createConnection();
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
					
				}, 
				autoAck: false);
		    return new SubscriberHandle(() =>
		    {
				s.Stop(TimeSpan.FromSeconds(30));
		    });
	    }


	}
}
