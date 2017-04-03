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
    public sealed class ResolvedEventMessageHandler<TSubscriber> : IMessageHandler<ResolvedEvent, Task<ResolvedEvent>> where TSubscriber : IMessageHandler
    {
        private readonly IMessageHandler _subscriber;

        private ResolvedEventMessageHandler(IMessageHandler subscriber) 
        {
            _subscriber = subscriber;
        }

        public Task<ResolvedEvent> Handle(ResolvedEvent message)
        {
            return Handle(_subscriber, message);
        }

        private async static Task<ResolvedEvent> Handle(IMessageHandler subscriber, ResolvedEvent resolvedEvent)
        {
            var eventHandlingTypes = subscriber
                .GetType()
                .GetMessageHandlerTypes()
                .Select(x => x.GetGenericArguments()[0].GetGenericArguments()[0]);
            var @event = DeserializeEvent(eventHandlingTypes, resolvedEvent);
            await Handle(subscriber, (dynamic)@event);
            return resolvedEvent;
        }

        private static Task Handle<TRecordedEvent>(IMessageHandler subscriber, TRecordedEvent recordedEvent)
        {
            var handler = (IMessageHandler<TRecordedEvent, Task>)subscriber;
            return handler.Handle(recordedEvent);
        }

        private static object DeserializeEvent(IEnumerable<Type> eventTypes, ResolvedEvent resolvedEvent)
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

        public static implicit operator ResolvedEventMessageHandler<TSubscriber>(TSubscriber subscriber)
        {
            return new ResolvedEventMessageHandler<TSubscriber>(subscriber);
        }
    }
}
