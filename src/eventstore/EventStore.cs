using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EventStore.ClientAPI;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace eventstore
{
	public struct EventDataSettings
	{ 
		public readonly Guid EventId;
		public readonly string EventName;
		public readonly IDictionary<string, object> EventHeader;

		private EventDataSettings(Guid eventId, string eventName, IDictionary<string, object> eventHeader)
		{
			EventName = eventName;
			EventId = eventId;
			EventHeader = eventHeader;
		}

		public EventDataSettings SetEventId(Guid eventId)
		{
			return new EventDataSettings(eventId, EventName, EventHeader);
		}

		public EventDataSettings SetEventName(string eventName)
		{
			return new EventDataSettings(EventId, eventName, EventHeader);
		}

		public EventDataSettings SetEventHeader(string key, object value)
		{
			return new EventDataSettings(EventId, EventName, new Dictionary<string, object>(EventHeader) { [key] = value });
		}

		public static EventDataSettings Create(Guid eventId, string eventName)
		{
			return new EventDataSettings(eventId, eventName, new Dictionary<string, object>());
		}
	}

	public interface IEventStore
	{
		Task<object[]> ReadEventsForward(string streamName, long fromEventNumber = 0);
		Task<WriteResult> WriteEvents(string streamName, long streamExpectedVersion, IEnumerable<object> events, Func<EventDataSettings, EventDataSettings> configureEventDataSettings = null);
	}

	public class EventStore : IEventStore
	{
		private const int _defaultSliceSize = 10;
		private static readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None };
		private readonly IEventStoreConnection _eventStoreConnection;

		public EventStore(IEventStoreConnection eventStoreConnection)
		{
			_eventStoreConnection = eventStoreConnection;
		}

		public async Task<object[]> ReadEventsForward(string streamName, long fromEventNumber)
		{
            var resolvedEvents = await ReadResolvedEvents(streamName, fromEventNumber).ConfigureAwait(false);
			return resolvedEvents.Select(DeserializeEvent).ToArray();
		}

		public Task<WriteResult> WriteEvents(string streamName, long streamExpectedVersion, IEnumerable<object> events, Func<EventDataSettings, EventDataSettings> configureEventDataSettings)
		{
			configureEventDataSettings = configureEventDataSettings ?? (x => x);
			var eventData = events
				.Select(@event =>
					ConvertToEventData(@event, configureEventDataSettings(
						EventDataSettings.Create(Guid.NewGuid(), @event.GetType().Name.ToLower())
							.SetEventHeader(EventHeaderKey.ClrType, @event.GetType().AssemblyQualifiedName)
							.SetEventHeader(EventHeaderKey.Topics, @event.GetType().GetEventTopics())))
				);
			return _eventStoreConnection.AppendToStreamAsync(streamName, streamExpectedVersion, eventData);
		}

		private async Task<IEnumerable<ResolvedEvent>> ReadResolvedEvents(string streamName, long fromEventNumber)
		{
			var resolvedEvents = new List<ResolvedEvent>();

			StreamEventsSlice slice;
			do
			{
				slice = await _eventStoreConnection.ReadStreamEventsForwardAsync(streamName, fromEventNumber, _defaultSliceSize, false).ConfigureAwait(false);
				if (slice.Status == SliceReadStatus.StreamNotFound)
				{
					throw new Exception($"Stream {streamName} not found.");
				}
				if (slice.Status == SliceReadStatus.StreamDeleted)
				{
					throw new Exception($"Stream {streamName} has been deleted.");
				}
				resolvedEvents.AddRange(slice.Events);
				fromEventNumber += _defaultSliceSize;
			} while (!slice.IsEndOfStream);

			return resolvedEvents;
		}

		private static EventData ConvertToEventData(object @event, EventDataSettings eventDataSettings)
		{
			var eventData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(@event, _serializerSettings));
			var eventMetadata = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(eventDataSettings.EventHeader, _serializerSettings));
			return new EventData(eventDataSettings.EventId, eventDataSettings.EventName, true, eventData, eventMetadata);
		}

		private static object DeserializeEvent(ResolvedEvent resolvedEvent)
		{
			var recordedEvent = resolvedEvent.Event;
			var eventMetadata = JObject.Parse(Encoding.UTF8.GetString(recordedEvent.Metadata));
			var eventClrTypeName = (string)eventMetadata.Property(EventHeaderKey.ClrType).Value;
			return JsonConvert.DeserializeObject(Encoding.UTF8.GetString(recordedEvent.Data), Type.GetType(eventClrTypeName));
		}
	}
}
