using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Threading;
using shared;

namespace infra
{
	public interface IEventStore
	{
		Task<IReadOnlyCollection<IEvent>> ReadEventsForwardAsync(string streamName, int fromEventNumber = 0);
		Task<WriteResult> WriteEventsAsync(string streamName, int streamExpectedVersion, IEnumerable<IEvent> events, Action<IEvent, IDictionary<string, object>> beforeSavingEvent = null);
	}

	public class EventStore : IEventStore
	{
		private const int _defaultSliceSize = 10;
		private static readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None };
		private readonly IEventStoreConnection _eventStoreConnection;
		private const string _eventClrTypeHeader = "EventClrType";

		public EventStore(IEventStoreConnection eventStoreConnection)
		{
			_eventStoreConnection = eventStoreConnection;
		}

		public async Task<IReadOnlyCollection<IEvent>> ReadEventsForwardAsync(string streamName, int fromEventNumber)
		{
            var resolvedEvents = await ReadResolvedEventsAsync(streamName, fromEventNumber)
                .ConfigureAwait(false);

            return resolvedEvents
                .Select(DeserializeEvent)
				.ToArray();
		}

        public Task<WriteResult> WriteEventsAsync(string streamName, int streamExpectedVersion, IEnumerable<IEvent> events, Action<IEvent, IDictionary<string, object>> beforeSavingEvent = null)
		{
            var eventsData = events
                .Select(@event =>
                {
                    var eventHeader = new Dictionary<string, object>
                    {
                        { _eventClrTypeHeader, @event.GetType().AssemblyQualifiedName },
                        { "topics", @event.GetEventTopics() }
                    };
                    beforeSavingEvent?.Invoke(@event, eventHeader);
                    return ConvertToEventData(@event, eventHeader);
                });
            return _eventStoreConnection.AppendToStreamAsync(streamName, streamExpectedVersion, eventsData);

        }

        private async Task<IReadOnlyCollection<ResolvedEvent>> ReadResolvedEventsAsync(string streamName, int fromEventNumber)
		{
			var resolvedEvents = new List<ResolvedEvent>();

			StreamEventsSlice slice;
			do
			{
				slice = await _eventStoreConnection.ReadStreamEventsForwardAsync(streamName, fromEventNumber, _defaultSliceSize, false).ConfigureAwait(false);
				if (slice.Status == SliceReadStatus.StreamNotFound)
				{
					throw new Exception($"stream {streamName} not found");
				}
				if (slice.Status == SliceReadStatus.StreamDeleted)
				{
					throw new Exception($"stream {streamName} has been deleted");
				}
				resolvedEvents.AddRange(slice.Events);
                fromEventNumber += _defaultSliceSize;
            } while (!slice.IsEndOfStream);

			return resolvedEvents;
		}

        private static EventData ConvertToEventData(IEvent @event, IDictionary<string, object> eventHeader)
        {
            var eventType = @event.GetType();
            var eventData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(@event, _serializerSettings));
            var eventMetadata = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(eventHeader, _serializerSettings));
            return new EventData(Guid.NewGuid(), eventType.Name.ToLower(), true, eventData, eventMetadata);
        }

        private static IEvent DeserializeEvent(ResolvedEvent resolvedEvent)
		{
			var recordedEvent = resolvedEvent.Event;
			var eventMetadata = JObject.Parse(Encoding.UTF8.GetString(recordedEvent.Metadata));
			var eventClrTypeName = (string)eventMetadata.Property(_eventClrTypeHeader).Value;
			return (IEvent)JsonConvert.DeserializeObject(Encoding.UTF8.GetString(recordedEvent.Data), Type.GetType(eventClrTypeName));
		}

	}
}
