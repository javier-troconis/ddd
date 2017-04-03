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
	public sealed class ResolvedEventHandler<TSubscriber> : IMessageHandler<ResolvedEvent, Task<ResolvedEvent>> where TSubscriber : IMessageHandler
	{
		private readonly Func<ResolvedEvent, Task<ResolvedEvent>> _handleResolvedEvent;

		private ResolvedEventHandler(IMessageHandler subscriber)
		{
			_handleResolvedEvent = CreateRecordedEventHandle(subscriber);
		}

		public Task<ResolvedEvent> Handle(ResolvedEvent message)
		{
			return _handleResolvedEvent(message);
		}

		private static Func<ResolvedEvent, Task<ResolvedEvent>> CreateRecordedEventHandle(IMessageHandler subscriber)
		{
			var eventHandlingTypes = subscriber
				.GetType()
				.GetMessageHandlerTypes()
				.Select(x => x.GetGenericArguments()[0].GetGenericArguments()[0]);

			Func<ResolvedEvent, object> deserializeEvent =
				resolvedEvent =>
				{
					var eventMetadata = JsonConvert.DeserializeObject<Dictionary<string, object>>(Encoding.UTF8.GetString(resolvedEvent.Event.Metadata));
					var topics = ((JArray)eventMetadata[EventHeaderKey.Topics]).ToObject<object[]>();
					var eventType = topics.Join(eventHandlingTypes, x => x, x => x.GetEventStoreName(), (x, y) => y).First();
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
				};

			return async resolvedEvent =>
			{
				var recordedEvent = deserializeEvent(resolvedEvent);
				await HandleRecordedEvent(subscriber, (dynamic)recordedEvent);
				return resolvedEvent;
			};
		}

		private static Task HandleRecordedEvent<TRecordedEvent>(IMessageHandler subscriber, TRecordedEvent recordedEvent)
		{
			var handler = (IMessageHandler<TRecordedEvent, Task>)subscriber;
			return handler.Handle(recordedEvent);
		}

		public static implicit operator ResolvedEventHandler<TSubscriber>(TSubscriber subscriber)
		{
			return new ResolvedEventHandler<TSubscriber>(subscriber);
		}
	}
}
