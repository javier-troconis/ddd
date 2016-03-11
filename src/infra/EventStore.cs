using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using shared;

namespace infra
{
	public class EventStore : IEventStore
	{
		private const int DefaultSliceSize = 10;
		private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None };
		private readonly IEventStoreConnection _eventStoreConnection;
		private const string EventClrTypeHeader = "EventClrType";

		public EventStore(IEventStoreConnection eventStoreConnection)
		{
			_eventStoreConnection = eventStoreConnection;
		}

		public async Task<IReadOnlyList<Event>> GetEventsAsync(string streamName)
		{
			var resolvedEvents = new List<ResolvedEvent>();

			StreamEventsSlice slice;
			do
			{
				slice = await _eventStoreConnection.ReadStreamEventsForwardAsync(streamName, resolvedEvents.Count, DefaultSliceSize, false).ConfigureAwait(false);

				if (slice.Status == SliceReadStatus.StreamNotFound)
				{
					throw new Exception($"stream {streamName} not found");
				}
				if (slice.Status == SliceReadStatus.StreamDeleted)
				{
					throw new Exception($"stream {streamName} has been deleted");
				}

				resolvedEvents.AddRange(slice.Events);

			} while (!slice.IsEndOfStream);

			return resolvedEvents.Select(DeserializeEvent).ToArray();
		}

		public async Task SaveEventsAsync(string streamName, int expectedVersion, IEnumerable<Event> events)
		{
			await _eventStoreConnection
				.AppendToStreamAsync(streamName, expectedVersion, events.Select(ToEventData))
				.ConfigureAwait(false);
		}

		private static EventData ToEventData(Event @event)
		{
			var eventType = @event.GetType();
			var eventData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(@event, SerializerSettings));
			var eventHeader = new Dictionary<string, object>
			{
				{EventClrTypeHeader, eventType.AssemblyQualifiedName}
			};
			var metadata = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(eventHeader, SerializerSettings));
			return new EventData(@event.EventId, eventType.Name.ToLower(), true, eventData, metadata);
		}

		private static Event DeserializeEvent(ResolvedEvent resolvedEvent)
		{
			var recordedEvent = resolvedEvent.Event;
			try
			{
				var eventMetadata = JObject.Parse(Encoding.UTF8.GetString(recordedEvent.Metadata));
				var eventClrTypeName = (string)eventMetadata.Property(EventClrTypeHeader).Value;
				return (Event)JsonConvert.DeserializeObject(Encoding.UTF8.GetString(recordedEvent.Data), Type.GetType(eventClrTypeName));
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException(
					$"Deserialization error: Could not deserialize event of type {recordedEvent.EventType} from stream {recordedEvent.EventStreamId} with ID: {recordedEvent.EventId}", ex);
			}
		}

	}
}
