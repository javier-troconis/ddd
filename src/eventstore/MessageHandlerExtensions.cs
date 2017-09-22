using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using ImpromptuInterface;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using shared;

namespace eventstore
{
	public static class MessageHandlerExtensions
	{
		//private static readonly Func<Type, ResolvedEvent, object> TryDeserializeEventWithCaching =
		//	new Func<Type, ResolvedEvent, object>(TryDeserializeEvent)
		//		.Memoize(
		//			new MemoryCache(new MemoryCacheOptions()),
		//			new MemoryCacheEntryOptions()
		//				.SetSlidingExpiration(TimeSpan.FromSeconds(5)),
		//			(eventType, resolvedEvent) => eventType == null ? string.Empty : eventType.FullName + resolvedEvent.Event.EventId);


		public static Func<ResolvedEvent, TOut> CreateResolvedEventHandler<TOut>(this IMessageHandler subscriber, TOut unHandledResult)
		{
			return new X<TOut>().CreateResolvedEventHandler(subscriber, unHandledResult);
			//TOut HandleEvent<TRecordedEvent>(TRecordedEvent recordedEvent)
			//{
			//	var handler = (IMessageHandler<TRecordedEvent, TOut>)subscriber;
			//	return handler.Handle(recordedEvent);
			//}

			//return resolvedEvent =>
			//{
			//	var eventHandlingTypes = subscriber
			//		.GetType()
			//		.GetMessageHandlerTypes()
			//		.Select(x => x.GetGenericArguments()[0].GetGenericArguments()[0])
			//		.ToArray();
			//	var eventMetadata = JsonConvert.DeserializeObject<Dictionary<string, object>>(Encoding.UTF8.GetString(resolvedEvent.Event.Metadata));
			//	var topics = ((JArray)eventMetadata[EventHeaderKey.Topics]).ToObject<object[]>();
			//	var eventType = topics.Join(eventHandlingTypes, x => x, x => x.GetEventStoreName(), (x, y) => y).FirstOrDefault();
			//	var recordedEvent = TryDeserializeEventWithCaching(eventType, resolvedEvent);
			//	if (recordedEvent != null)
			//	{
			//		return HandleEvent((dynamic)recordedEvent);
			//	}
			//	return unHandledResult;
			//};
		}

		//private static object TryDeserializeEvent(Type eventType, ResolvedEvent resolvedEvent)
		//{
		//	if (eventType == default(Type))
		//	{
		//		return null;
		//	}
		//	var recordedEvent = new
		//	{
		//		resolvedEvent.OriginalStreamId,
		//		resolvedEvent.OriginalEventNumber,
		//		resolvedEvent.Event.EventStreamId,
		//		resolvedEvent.Event.EventNumber,
		//		resolvedEvent.Event.EventId,
		//		resolvedEvent.Event.Created,
		//		Data = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(resolvedEvent.Event.Data))
		//	};
		//	var recordedEventType = typeof(IRecordedEvent<>).MakeGenericType(eventType);
		//	return Impromptu.CoerceConvert(recordedEvent, recordedEventType);
		//}

		private class X<TOut>
		{
			private static readonly Func<Type, ResolvedEvent, object> TryDeserializeEventWithCaching =
				new Func<Type, ResolvedEvent, object>(TryDeserializeEvent)
					.Memoize(
						new MemoryCache(new MemoryCacheOptions()),
						new MemoryCacheEntryOptions()
							.SetSlidingExpiration(TimeSpan.FromSeconds(5)),
						(eventType, resolvedEvent) => eventType == null ? string.Empty : eventType.FullName + resolvedEvent.Event.EventId);


			public Func<ResolvedEvent, TOut> CreateResolvedEventHandler(IMessageHandler subscriber, TOut unHandledResult)
			{
				var eventHandlingTypes = subscriber
					.GetType()
					.GetMessageHandlerTypes()
					.Select(x => x.GetGenericArguments()[0].GetGenericArguments()[0])
					.ToArray();

				return resolvedEvent =>
				{
					var eventMetadata = JsonConvert.DeserializeObject<Dictionary<string, object>>(Encoding.UTF8.GetString(resolvedEvent.Event.Metadata));
					var topics = ((JArray)eventMetadata[EventHeaderKey.Topics]).ToObject<object[]>();
					var eventType = topics.Join(eventHandlingTypes, x => x, x => x.GetEventStoreName(), (x, y) => y).FirstOrDefault();
					var recordedEvent = TryDeserializeEventWithCaching(eventType, resolvedEvent);
					if (recordedEvent != null)
					{
						return HandleEvent(subscriber, (dynamic)recordedEvent);
					}
					return unHandledResult;
				};
			}

			private static TOut HandleEvent<TRecordedEvent>(IMessageHandler subscriber, TRecordedEvent recordedEvent)
			{
				var handler = (IMessageHandler<TRecordedEvent, TOut>)subscriber;
				return handler.Handle(recordedEvent);
			}

			private static object TryDeserializeEvent(Type eventType, ResolvedEvent resolvedEvent)
			{
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
					resolvedEvent.Event.EventId,
					resolvedEvent.Event.Created,
					Data = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(resolvedEvent.Event.Data))
				};
				var recordedEventType = typeof(IRecordedEvent<>).MakeGenericType(eventType);
				return Impromptu.CoerceConvert(recordedEvent, recordedEventType);
			}
		}
	}
}
