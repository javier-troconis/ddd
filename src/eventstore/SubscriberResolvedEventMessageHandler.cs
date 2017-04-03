using EventStore.ClientAPI;
using shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace eventstore
{



    public sealed class ResolvedEventMessageHandler<TSubscriber> : IMessageHandler<ResolvedEvent, Task<ResolvedEvent>> where TSubscriber : IMessageHandler
    {
        private readonly IMessageHandler _subscriber;

        public ResolvedEventMessageHandler(IMessageHandler subscriber) 
        {
            _subscriber = subscriber;
        }

        public Task<ResolvedEvent> Handle(ResolvedEvent message)
        {
            return SubscriberResolvedEventHandleFactory.CreateSubscriberResolvedEventHandle(_subscriber, message);
        }

        public static implicit operator ResolvedEventMessageHandler<TSubscriber>(TSubscriber subscriber)
        {
            return new ResolvedEventMessageHandler<TSubscriber>(subscriber);
        }


    }
}
