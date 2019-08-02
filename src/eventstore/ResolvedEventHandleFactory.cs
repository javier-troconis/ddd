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
using Dynamitey;
using System.Collections.ObjectModel;

namespace eventstore
{
    public static class ResolvedEventHandleFactory
    {
		//private static readonly Func<Type, ResolvedEvent, object> TryDeserializeEventWithCaching =
		//		new Func<Type, ResolvedEvent, object>(TryDeserializeEvent)
		//			.Memoize(
		//				new MemoryCache(new MemoryCacheOptions()),
		//				new MemoryCacheEntryOptions()
		//					.SetSlidingExpiration(TimeSpan.FromSeconds(5)),
		//				(eventType, resolvedEvent) => eventType == null ? string.Empty : eventType.FullName + resolvedEvent.Event.EventId);

		public static Func<T1, ResolvedEvent, T2> CreateResolvedEventHandle<T1, T2>(Func<T1, ResolvedEvent, T2> getResultForUnhandledMessage) where T1 : IMessageHandler
		{
			var candidateEventTypes = typeof(T1)
				.GetMessageHandlerTypes()
				.Select(x => x.GetGenericArguments()[0].GetGenericArguments()[0]);
			return
				(handler, resolvedEvent) =>
				{
					var recordedEvent = TryDeserializeEvent(candidateEventTypes, resolvedEvent);
					return recordedEvent == null
						? getResultForUnhandledMessage(handler, resolvedEvent)
						: EventHandler<T2>.HandleEvent(handler, (dynamic)recordedEvent);
				};
		}

		public static Func<T1, ResolvedEvent, T1> CreateResolvedEventHandle<T1>() where T1 : IMessageHandler
		{
			return CreateResolvedEventHandle<T1, T1>((handler, resolvedEvent) => handler);
		}

		private static object TryDeserializeEvent(IEnumerable<Type> candidateEventTypes, ResolvedEvent resolvedEvent)
	    {
		    var eventMetadata = Json.ParseJson<Dictionary<string, object>>(resolvedEvent.Event.Metadata);
		    var topics = ((JArray)eventMetadata[EventHeaderKey.Topics]).ToObject<string[]>();
		    var eventType = topics
			    .Join(candidateEventTypes, x => x, x => ((EventStoreObjectName)x).Value, (x, y) => y)
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
                CorrelationId = eventMetadata.TryGetValue(EventHeaderKey.CorrelationId, out var correlationId) ? Guid.Parse((string)correlationId) : default(Guid?),
                Metadata = eventMetadata.SkipWhile(x => x.Key == EventHeaderKey.CorrelationId || x.Key == EventHeaderKey.Topics).ToDictionary(x => x.Key, x => x.Value),
				Data = Json.ParseJson<object>(resolvedEvent.Event.Data)
		    };

		    var recordedEventType = typeof(IRecordedEvent<>).MakeGenericType(eventType);
		    return Dynamic.CoerceConvert(recordedEvent, recordedEventType);
	    }

	    private static class EventHandler<T>
	    {
		    public static T HandleEvent<TRecordedEvent>(IMessageHandler handler, TRecordedEvent recordedEvent)
		    {
				return ((IMessageHandler<TRecordedEvent, T>)handler).Handle(recordedEvent);
		    }
	    }
	}
}
