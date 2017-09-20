using System;
using System.Collections.Generic;
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
		private static readonly Func<Type, ResolvedEvent, object> TryDeserializeEventWithCache =
			new Func<Type, ResolvedEvent, object>(TryDeserializeEvent)
				.Memoize(
					new MemoryCache(new MemoryCacheOptions()),
					new MemoryCacheEntryOptions()
						.SetSlidingExpiration(TimeSpan.FromSeconds(5))
				);


		public static Func<ResolvedEvent, Task<ResolvedEvent>> CreateResolvedEventHandler(this IMessageHandler subscriber)
		{
			var eventHandlingTypes = subscriber
				.GetType()
				.GetMessageHandlerTypes()
				.Select(x => x.GetGenericArguments()[0].GetGenericArguments()[0])
				.ToArray();

			return async resolvedEvent =>
			{
				var eventMetadata = JsonConvert.DeserializeObject<Dictionary<string, object>>(Encoding.UTF8.GetString(resolvedEvent.Event.Metadata));
				var topics = ((JArray)eventMetadata[EventHeaderKey.Topics]).ToObject<object[]>();
				var eventType = topics.Join(eventHandlingTypes, x => x, x => x.GetEventStoreName(), (x, y) => y).FirstOrDefault();

				var recordedEvent = TryDeserializeEventWithCache(eventType, resolvedEvent);
				if (recordedEvent != null)
				{
					await HandleEvent(subscriber, (dynamic)recordedEvent);
				}
				return resolvedEvent;
			};
		}

		private static object TryDeserializeEvent(Type eventType, ResolvedEvent resolvedEvent)
		{
			if (eventType == null)
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

		private static Task HandleEvent<TRecordedEvent>(IMessageHandler subscriber, TRecordedEvent recordedEvent)
		{
			var handler = (IMessageHandler<TRecordedEvent, Task>)subscriber;
			return handler.Handle(recordedEvent);
		}
	}
}
