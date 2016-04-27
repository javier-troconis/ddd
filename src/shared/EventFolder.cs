using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace shared
{
    public static class EventFolder
    {
		public static TEventConsumer Fold<TEventConsumer>(TEventConsumer eventConsumer, IEvent @event) where TEventConsumer : IEventConsumer
		{
			return Fold(eventConsumer, (dynamic)@event);
		}

		public static TEventConsumer Fold<TEventConsumer, TEvent>(TEventConsumer eventConsumer, TEvent @event) where TEventConsumer : IEventConsumer where TEvent : IEvent
		{
			var specificEventConsumer = eventConsumer as IEventConsumer<TEvent, TEventConsumer>;
			return specificEventConsumer == null ? eventConsumer : specificEventConsumer.Apply(@event);
		}
	}
}
