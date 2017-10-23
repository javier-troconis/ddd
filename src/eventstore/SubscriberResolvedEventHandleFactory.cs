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
using System.Collections.ObjectModel;

namespace eventstore
{
    public static class SubscriberResolvedEventHandleFactory
    {
	    //private static readonly Func<Type, ResolvedEvent, object> TryDeserializeEventWithCaching =
	    //		new Func<Type, ResolvedEvent, object>(TryDeserializeEvent)
	    //			.Memoize(
	    //				new MemoryCache(new MemoryCacheOptions()),
	    //				new MemoryCacheEntryOptions()
	    //					.SetSlidingExpiration(TimeSpan.FromSeconds(5)),
	    //				(eventType, resolvedEvent) => eventType == null ? string.Empty : eventType.FullName + resolvedEvent.Event.EventId);

		public static Func<TSubscriber, ResolvedEvent, TResult> CreateSubscriberResolvedEventHandle<TSubscriber, TResult>(Func<TSubscriber, ResolvedEvent, TResult> getUnHandledResult) where TSubscriber : IMessageHandler
		{
			var candidateEventTypes = typeof(TSubscriber)
				.GetMessageHandlerTypes()
				.Select(x => x.GetGenericArguments()[0].GetGenericArguments()[0]);
		    return
			    (subscriber, resolvedEvent) =>
			    {
				    var recordedEvent = TryDeserializeEvent(candidateEventTypes, resolvedEvent);
				    return recordedEvent == null
					    ? getUnHandledResult(subscriber, resolvedEvent)
					    : EventHandler<TResult>.HandleEvent(subscriber, (dynamic)recordedEvent);
			    };
	    }

	    private static object TryDeserializeEvent(IEnumerable<Type> candidateEventTypes, ResolvedEvent resolvedEvent)
	    {
		    var eventMetadata = JsonConvert.DeserializeObject<Dictionary<string, object>>(Encoding.UTF8.GetString(resolvedEvent.Event.Metadata));
		    var topics = ((JArray)eventMetadata[EventHeaderKey.Topics]).ToObject<string[]>();
		    var eventType = topics
			    .Join(candidateEventTypes, x => x, x => x.GetEventStoreName(), (x, y) => y)
			    .FirstOrDefault();
			if (eventType == default(Type))
		    {
			    return null;
		    }
		    var recordedEvent = new
		    {
                resolvedEvent.OriginalStreamId,
                resolvedEvent.OriginalEventNumber,
                resolvedEvent.Event.EventStreamId,
                resolvedEvent.Event.EventNumber,
                resolvedEvent.Event.Created,
                resolvedEvent.Event.EventId,
                CorrelationId = eventMetadata.TryGetValue(EventHeaderKey.CorrelationId, out object correlationId) ? Guid.Parse((string)correlationId) : default(Guid?),
                Metadata = eventMetadata.SkipWhile(x => x.Key == EventHeaderKey.CorrelationId || x.Key == EventHeaderKey.Topics).ToDictionary(x => x.Key, x => x.Value),
				Data = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(resolvedEvent.Event.Data))
		    };

		    var recordedEventType = typeof(IRecordedEvent<>).MakeGenericType(eventType);
		    return Impromptu.CoerceConvert(recordedEvent, recordedEventType);
	    }

	    private static class EventHandler<TOut>
	    {
		    public static TOut HandleEvent<TRecordedEvent>(IMessageHandler subscriber, TRecordedEvent recordedEvent)
		    {
			    var handler = (IMessageHandler<TRecordedEvent, TOut>)subscriber;
			    return handler.Handle(recordedEvent);
		    }
	    }
	}
}
