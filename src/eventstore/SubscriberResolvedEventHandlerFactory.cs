using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using ImpromptuInterface;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using shared;

namespace eventstore
{
    public static class SubscriberResolvedEventHandlerFactory
    {
	    public static Func<ResolvedEvent, Task<ResolvedEvent>> CreateSubscriberResolvedEventHandler(IMessageHandler subscriber)
	    {
		    var eventHandlingTypes = subscriber
			    .GetType()
			    .GetMessageHandlerTypes()
			    .Select(x => x.GetGenericArguments()[0].GetGenericArguments()[0])
			    .ToArray();

		    return async resolvedEvent =>
		    {
			    var recordedEvent = TryDeserializeEvent(eventHandlingTypes, resolvedEvent);
			    if (recordedEvent != null)
			    {
				    await HandleEvent(subscriber, (dynamic) recordedEvent);
			    }
			    return resolvedEvent;
		    };
	    }

	    private static object TryDeserializeEvent(IEnumerable<Type> eventTypes, ResolvedEvent resolvedEvent)
	    {
		    var eventMetadata = JsonConvert.DeserializeObject<Dictionary<string, object>>(Encoding.UTF8.GetString(resolvedEvent.Event.Metadata));
		    var topics = ((JArray)eventMetadata[EventHeaderKey.Topics]).ToObject<object[]>();
		    var eventType = topics.Join(eventTypes, x => x, x => x.GetEventStoreName(), (x, y) => y).FirstOrDefault();
		    if (eventType == null)
		    {
			    return null;
			    //throw new Exception($"Failed to deserialized event for topic {topics[0]}. The messagehandler for topic {topics[0]} has been removed from the subscriber but the subscription stream has not been updated.");
		    }
		    var recordedEvent = new
		    {
			    resolvedEvent.OriginalStreamId,
			    resolvedEvent.OriginalEventNumber,
			    resolvedEvent.Event.EventStreamId,
			    resolvedEvent.Event.EventNumber,
			    resolvedEvent.Event.EventId,
			    resolvedEvent.Event.Created,
			    Data = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(resolvedEvent.Event.Data))
		    };
		    var recordedEventType = typeof(IRecordedEvent<>).MakeGenericType(eventType);
		    return Impromptu.CoerceConvert(recordedEvent, recordedEventType);
	    }

	    private static Task HandleEvent<TRecordedEvent>(IMessageHandler subscriber, TRecordedEvent recordedEvent)
	    {
		    var handler = (IMessageHandler<TRecordedEvent, Task>)subscriber;
		    return handler.Handle(recordedEvent);
	    }
	}
}
