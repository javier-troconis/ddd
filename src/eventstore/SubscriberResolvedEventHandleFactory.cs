using EventStore.ClientAPI;
using ImpromptuInterface;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eventstore
{
    public static class SubscriberResolvedEventHandleFactory
    {
        public async static Task<ResolvedEvent> CreateSubscriberResolvedEventHandle(IMessageHandler subscriber, ResolvedEvent resolvedEvent)
        {
         
                var eventHandlingTypes = subscriber
                    .GetType()
                    .GetMessageHandlerTypes()
                    .Select(x => x.GetGenericArguments()[0].GetGenericArguments()[0]);
                var recordedEvent = DeserializeRecordedEvent(eventHandlingTypes, resolvedEvent);
                await HandleSubscriberRecordedEvent(subscriber, (dynamic)recordedEvent);
                return resolvedEvent;
            
        }

        private static Task HandleSubscriberRecordedEvent<TRecordedEvent>(IMessageHandler subscriber, TRecordedEvent recordedEvent)
        {
            var handler = (IMessageHandler<TRecordedEvent, Task>)subscriber;
            return handler.Handle(recordedEvent);
        }

        private static object DeserializeRecordedEvent(IEnumerable<Type> eventTypes, ResolvedEvent resolvedEvent)
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
