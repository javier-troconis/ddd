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
		Task<IReadOnlyCollection<IEvent>> ReadEventsAsync(string streamName);
		Task<WriteResult> WriteEventsAsync(string streamName, int streamExpectedVersion, IEnumerable<IEvent> events, IDictionary<string, object> eventHeader = null);
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

		public async Task<IReadOnlyCollection<IEvent>> ReadEventsAsync(string streamName)
		{
			var resolvedEvents = await ReadResolvedEventsAsync(streamName).ConfigureAwait(false);
			return resolvedEvents
				.Select(DeserializeEvent)
				.ToArray();
		}

		public async Task<WriteResult> WriteEventsAsync(string streamName, int streamExpectedVersion, IEnumerable<IEvent> events, IDictionary<string, object> eventHeader = null)
		{
            eventHeader = eventHeader ?? new Dictionary<string, object>();
			var eventData = events
                .Select(@event => new Tuple<IDictionary<string, object>, IEvent>(eventHeader.ToDictionary(x => x.Key, x => x.Value), @event))
				.Select(ConfigureEventHeader)
				.Select(ConvertToEventData);
			return await _eventStoreConnection.AppendToStreamAsync(streamName, streamExpectedVersion, eventData).ConfigureAwait(false);
		}

		private async Task<IReadOnlyCollection<ResolvedEvent>> ReadResolvedEventsAsync(string streamName)
		{
			var result = new List<ResolvedEvent>();

			StreamEventsSlice slice;
			do
			{
				slice = await _eventStoreConnection.ReadStreamEventsForwardAsync(streamName, result.Count, DefaultSliceSize, false).ConfigureAwait(false);
				if (slice.Status == SliceReadStatus.StreamNotFound)
				{
					throw new Exception($"stream {streamName} not found");
				}
				if (slice.Status == SliceReadStatus.StreamDeleted)
				{
					throw new Exception($"stream {streamName} has been deleted");
				}
				result.AddRange(slice.Events);
			} while (!slice.IsEndOfStream);

			return result;
		}

		private static Tuple<IDictionary<string, object>, IEvent> ConfigureEventHeader(Tuple<IDictionary<string, object>, IEvent> arg)
		{
			var eventType = arg.Item2.GetType();
            arg.Item1[EventClrTypeHeader] = eventType.AssemblyQualifiedName;
			return arg;
		}

		private static EventData ConvertToEventData(Tuple<IDictionary<string, object>, IEvent> arg)
		{
			var eventId = Guid.NewGuid();
			var eventType = arg.Item2.GetType();
			var eventData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(arg.Item2, SerializerSettings));
			var eventMetadata = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(arg.Item1, SerializerSettings));
			return new EventData(eventId, eventType.Name.ToLower(), true, eventData, eventMetadata);
		}

		private static IEvent DeserializeEvent(ResolvedEvent resolvedEvent)
		{
			var recordedEvent = resolvedEvent.Event;
			var eventMetadata = JObject.Parse(Encoding.UTF8.GetString(recordedEvent.Metadata));
			var eventClrTypeName = (string)eventMetadata.Property(EventClrTypeHeader).Value;
			return (IEvent)JsonConvert.DeserializeObject(Encoding.UTF8.GetString(recordedEvent.Data), Type.GetType(eventClrTypeName));
		}

	}
}
