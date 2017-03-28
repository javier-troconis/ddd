﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EventStore.ClientAPI;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace eventstore
{
	public interface IEventStore
	{
		Task<object[]> ReadEventsForward(string streamName, long fromEventNumber = 0);
		Task<WriteResult> WriteEvents(string streamName, long streamExpectedVersion, IEnumerable<object> events, Action<object, IDictionary<string, object>> configureEventHeader = null);
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

		public async Task<object[]> ReadEventsForward(string streamName, long fromEventNumber)
		{
            var resolvedEvents = await ReadResolvedEvents(streamName, fromEventNumber).ConfigureAwait(false);
			return resolvedEvents.Select(DeserializeEvent).ToArray();
		}

        public Task<WriteResult> WriteEvents(string streamName, long streamExpectedVersion, IEnumerable<object> events, Action<object, IDictionary<string, object>> configureEventHeader)
		{
            var eventsData = events
                .Select(@event =>
                {
                    var eventType = @event.GetType();
                    var eventHeader = new Dictionary<string, object>
                    {
                        { _eventClrTypeHeader, eventType.AssemblyQualifiedName },
                        { EventHeaderKey.Topics, eventType.GetEventTopics() }
                    };
                    configureEventHeader?.Invoke(@event, eventHeader);
                    return ConvertToEventData(@event, eventHeader);
                });
            return _eventStoreConnection.AppendToStreamAsync(streamName, streamExpectedVersion, eventsData);

        }

        private async Task<IReadOnlyCollection<ResolvedEvent>> ReadResolvedEvents(string streamName, long fromEventNumber)
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

        private static EventData ConvertToEventData(object @event, IDictionary<string, object> eventHeader)
        {
            var eventType = @event.GetType();
            var eventData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(@event, _serializerSettings));
            var eventMetadata = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(eventHeader, _serializerSettings));
            return new EventData(Guid.NewGuid(), eventType.Name.ToLower(), true, eventData, eventMetadata);
        }

        private static object DeserializeEvent(ResolvedEvent resolvedEvent)
		{
			var recordedEvent = resolvedEvent.Event;
			var eventMetadata = JObject.Parse(Encoding.UTF8.GetString(recordedEvent.Metadata));
			var eventClrTypeName = (string)eventMetadata.Property(_eventClrTypeHeader).Value;
			return JsonConvert.DeserializeObject(Encoding.UTF8.GetString(recordedEvent.Data), Type.GetType(eventClrTypeName));
		}

	}
}
