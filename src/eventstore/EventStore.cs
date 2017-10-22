using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using shared;
using EventStore.ClientAPI;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace eventstore
{
	public struct EventHeader
	{ 
		public readonly Guid EventId;
        public readonly string EventType;
        public readonly IReadOnlyDictionary<string, object> CustomValues;
  
		private EventHeader(Guid eventId, string eventType, IReadOnlyDictionary<string, object> customValues)
		{
			EventId = eventId;
            EventType = eventType;
            CustomValues = customValues;
		}

		public EventHeader SetEventId(Guid eventId)
		{
			return new EventHeader(eventId, EventType, CustomValues);
		}

		public EventHeader SetCorrelationId(Guid correlationId)
		{
            return SetEntry(EventHeaderKey.CorrelationId, correlationId);
		}

		public EventHeader SetEntry(string key, object value)
		{
			return new EventHeader(
				EventId,
                EventType,
                new Dictionary<string, object>
				(
					new Dictionary<string, object>
					{
						{
							key, value
						}
					}.Merge(CustomValues.ToDictionary(x => x.Key, x => x.Value)))
				);
		}

		internal static EventHeader Create(Guid eventId, string eventType, string[] topics)
		{
            return new EventHeader
                (
                    eventId,
                    eventType,
                    new Dictionary<string, object>
                    {
                        {
                            EventHeaderKey.Topics, topics
                        }
                    }
                );
		}
	}

	public interface IEventStore
	{
		Task<IEnumerable<ResolvedEvent>> ReadEventsForward(string streamName, long fromEventNumber = 0);
		Task<WriteResult> WriteEvents(string streamName, long streamExpectedVersion, IEnumerable<object> events, Func<EventHeader, EventHeader> configureEventHeader = null);
		Task<WriteResult> WriteStreamMetadata(string streamName, long streamExpectedVersion, StreamMetadata metadata);
	}

	public class EventStore : IEventStore
	{
		private readonly IEventStoreConnection _eventStoreConnection;

		public EventStore(IEventStoreConnection eventStoreConnection)
		{
			_eventStoreConnection = eventStoreConnection;
		}

		public Task<WriteResult> WriteEvents(string streamName, long streamExpectedVersion, IEnumerable<object> events, Func<EventHeader, EventHeader> configureEventHeader)
		{
			var eventData = events
				.Select
				(
					@event =>
						ConvertToEventData
						(
							@event,
							(configureEventHeader ?? (x => x))
							(
								EventHeader.Create
								(
									Guid.NewGuid(),
                                    @event.GetType().FullName,
									@event.GetType().GetEventTopics()
								)
							)
						)
				);
			return _eventStoreConnection.AppendToStreamAsync(streamName, streamExpectedVersion, eventData);
		}

		public async Task<IEnumerable<ResolvedEvent>> ReadEventsForward(string streamName, long fromEventNumber)
		{
			const int defaultSliceSize = 10;

			var resolvedEvents = new List<ResolvedEvent>();

			StreamEventsSlice slice;
			do
			{
				slice = await _eventStoreConnection.ReadStreamEventsForwardAsync(streamName, fromEventNumber, defaultSliceSize, false).ConfigureAwait(false);
				if (slice.Status == SliceReadStatus.StreamNotFound)
				{
					throw new Exception($"Stream {streamName} not found.");
				}
				if (slice.Status == SliceReadStatus.StreamDeleted)
				{
					throw new Exception($"Stream {streamName} has been deleted.");
				}
				resolvedEvents.AddRange(slice.Events);
				fromEventNumber += defaultSliceSize;
			} while (!slice.IsEndOfStream);

			return resolvedEvents;
		}

		private static EventData ConvertToEventData(object @event, EventHeader eventHeader)
		{
			var serializerSettings = new JsonSerializerSettings
			{
				TypeNameHandling = TypeNameHandling.None
			};
			var eventData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(@event, serializerSettings));
			var eventMetadata = Encoding.UTF8.GetBytes
			(
				JsonConvert.SerializeObject
				(
					eventHeader.CustomValues,
					serializerSettings
				)
			);
			return new EventData(eventHeader.EventId, eventHeader.EventType, true, eventData, eventMetadata);
		}

		public Task<WriteResult> WriteStreamMetadata(string streamName, long streamExpectedVersion, StreamMetadata metadata)
		{
			return _eventStoreConnection.SetStreamMetadataAsync(streamName, streamExpectedVersion, metadata);
		}
	}
}
