using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EventStore.ClientAPI;

using ImpromptuInterface;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using shared;

namespace eventstore
{
    internal static class RecordedEventDeserializer
    {
		public static object DeserializeRecordedEvent(IEnumerable<Type> eventTypes, ResolvedEvent resolvedEvent)
		{
			var eventMetadata = JsonConvert.DeserializeObject<Dictionary<string, object>>(Encoding.UTF8.GetString(resolvedEvent.Event.Metadata));
			var topics = ((JArray)eventMetadata[EventHeaderKey.Topics]).ToObject<object[]>();
			var eventType = topics.Join(eventTypes, x => x, x => x.GetEventStoreName(), (x, y) => y).First();
			var recordedEvent = new
			{
				resolvedEvent.OriginalEventNumber,
				resolvedEvent.Event.EventStreamId,
				resolvedEvent.Event.EventNumber,
				resolvedEvent.Event.EventId,
				resolvedEvent.Event.Created,
				Event = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(resolvedEvent.Event.Data))
			};
			return Impromptu.CoerceConvert(recordedEvent, typeof(IRecordedEvent<>).MakeGenericType(eventType));
		}


	}
}
