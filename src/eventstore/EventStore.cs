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
	public struct EventConfiguration
	{ 
		public readonly Guid EventId;
        public readonly string EventType;       
        public readonly IReadOnlyDictionary<string, object> Metadata;
  
		private EventConfiguration(Guid eventId, string eventType, IReadOnlyDictionary<string, object> metaData)
		{
			EventId = eventId;
            EventType = eventType;
            Metadata = metaData;
		}

		public EventConfiguration SetEventId(Guid eventId)
		{
			return new EventConfiguration(eventId, EventType, Metadata);
		}

		public EventConfiguration SetCorrelationId(Guid correlationId)
		{
            return SetMetadata(EventHeaderKey.CorrelationId, correlationId);
        }

		public EventConfiguration SetMetadata(string key, object value)
		{
			return new EventConfiguration(
				EventId,
                EventType,
                new Dictionary<string, object>
				(
					new Dictionary<string, object>
					{
						{
							key, value
						}
					}.Merge(Metadata.ToDictionary(x => x.Key, x => x.Value)))
				);
		}

		public EventConfiguration CopyMetadata(IReadOnlyDictionary<string, object> metadata)
		{
			return metadata.Aggregate(this, (y, z) => y.SetMetadata(z.Key, z.Value));
		}

		internal static EventConfiguration Create(Guid eventId, string eventType, string[] topics)
		{
            return new EventConfiguration
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
		Task<WriteResult> WriteEvents(EventStoreObjectName streamName, long streamExpectedVersion, IEnumerable<object> events, Func<EventConfiguration, EventConfiguration> configureEvent = null);
		Task<WriteResult> WriteEvent(EventStoreObjectName streamName, long streamExpectedVersion, object @event, Func<EventConfiguration, EventConfiguration> configureEvent = null);
		Task<WriteResult> WriteStreamMetadata(EventStoreObjectName streamName, long streamExpectedVersion, StreamMetadata metadata);
	}

	public class EventStore : IEventStore
	{
		private readonly IEventStoreConnection _eventStoreConnection;

		public EventStore(IEventStoreConnection eventStoreConnection)
		{
			_eventStoreConnection = eventStoreConnection;
		}

		public Task<WriteResult> WriteEvents(EventStoreObjectName streamName, long streamExpectedVersion, IEnumerable<object> events, Func<EventConfiguration, EventConfiguration> configureEvent)
		{
			var eventData = events
				.Select
				(
					@event =>
						ConvertToEventData
						(
							@event,
                            EventConfiguration.Create
                            (
                                Guid.NewGuid(),
                                @event.GetType().FullName,
                                @event.GetType().GetEventTopics()
                            )
                            .PipeForward
                            (
                                configureEvent ?? (x => x)
                            )
						)
				);
			return _eventStoreConnection.AppendToStreamAsync(streamName, streamExpectedVersion, eventData);
		}

		public Task<WriteResult> WriteEvent(EventStoreObjectName streamName, long streamExpectedVersion, object @event, Func<EventConfiguration, EventConfiguration> configureEvent)
		{
			return WriteEvents(streamName, streamExpectedVersion, new[] {@event}, configureEvent);
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

		private static EventData ConvertToEventData(object @event, EventConfiguration eventConfiguration)
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
					eventConfiguration.Metadata,
					serializerSettings
				)
			);
			return new EventData(eventConfiguration.EventId, eventConfiguration.EventType, true, eventData, eventMetadata);
		}

		public Task<WriteResult> WriteStreamMetadata(EventStoreObjectName streamName, long streamExpectedVersion, StreamMetadata metadata)
		{
			return _eventStoreConnection.SetStreamMetadataAsync(streamName, streamExpectedVersion, metadata);
		}
	}
}
