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
	public interface IEventStore
	{
		Task<IReadOnlyList<Event>> ReadEventsAsync(string streamName);
		Task WriteEventsAsync(string streamName, int streamExpectedVersion, IEnumerable<Event> events, Action<IDictionary<string, object>> configureEventHeader = null);
	}

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

		public async Task<IReadOnlyList<Event>> ReadEventsAsync(string streamName)
		{
			var resolvedEvents = await GetResolvedEvents(streamName).ConfigureAwait(false);
			return resolvedEvents
				.Select(DeserializeEvent)
				.ToArray();
		}

		public async Task WriteEventsAsync(string streamName, int streamExpectedVersion, IEnumerable<Event> events, Action<IDictionary<string, object>> configureEventHeader = null)
		{
			var eventsData = events
				.Select(@event => CreateEventHeader(@event, configureEventHeader))
				.Select(ConvertToEventData);
			await _eventStoreConnection.AppendToStreamAsync(streamName, streamExpectedVersion, eventsData).ConfigureAwait(false);
		}

		private async Task<IReadOnlyList<ResolvedEvent>> GetResolvedEvents(string streamName)
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
			return resolvedEvents;
		}

		private static Tuple<IDictionary<string, object>, Event> CreateEventHeader(Event @event, Action<IDictionary<string, object>> configureEventHeader)
		{
			var eventType = @event.GetType();
			var eventHeader = new Dictionary<string, object>
			{
				{EventClrTypeHeader, eventType.AssemblyQualifiedName}
			};
			configureEventHeader?.Invoke(eventHeader);
			return new Tuple<IDictionary<string, object>, Event>(eventHeader, @event);
		}

		private static EventData ConvertToEventData(Tuple<IDictionary<string, object>, Event> arg)
		{
			var eventType = arg.Item2.GetType();
			var eventData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(arg.Item2, SerializerSettings));
			var eventMetadata = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(arg.Item1, SerializerSettings));
			return new EventData(arg.Item2.EventId, eventType.Name.ToLower(), true, eventData, eventMetadata);
		}

		private static Event DeserializeEvent(ResolvedEvent resolvedEvent)
		{
			var recordedEvent = resolvedEvent.Event;
			var eventMetadata = JObject.Parse(Encoding.UTF8.GetString(recordedEvent.Metadata));
			var eventClrTypeName = (string)eventMetadata.Property(EventClrTypeHeader).Value;
			return (Event)JsonConvert.DeserializeObject(Encoding.UTF8.GetString(recordedEvent.Data), Type.GetType(eventClrTypeName));
		}

	}
}
