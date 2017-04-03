using EventStore.ClientAPI;
using shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace eventstore
{
    public sealed class ResolvedEventMessageHandler : IMessageHandler<ResolvedEvent, Task<ResolvedEvent>>
    {
        private readonly Func<ResolvedEvent, Task<ResolvedEvent>> _handleResolvedEvent;

        public ResolvedEventMessageHandler(Func<ResolvedEvent, Task<ResolvedEvent>> handleResolvedEvent)
        {
            _handleResolvedEvent = handleResolvedEvent;
        }

        public Task<ResolvedEvent> Handle(ResolvedEvent message)
        {
            return _handleResolvedEvent(message);
        }
    }

    public sealed class ResolvedEventMessageHandler<TSubscriber> : IMessageHandler<ResolvedEvent, Task<ResolvedEvent>> where TSubscriber : IMessageHandler
    {
        private readonly TSubscriber _subscriber;

        public ResolvedEventMessageHandler(TSubscriber subscriber)
        {
            _subscriber = subscriber;
        }

        public Task<ResolvedEvent> Handle(ResolvedEvent message)
        {
            var handleResolvedEvent = SubscriberResolvedEventHandleFactory.CreateSubscriberResolvedEventHandle(_subscriber);
            return handleResolvedEvent(message);
        }

        public static implicit operator ResolvedEventMessageHandler<TSubscriber>(TSubscriber subscriber)
        {
            return new ResolvedEventMessageHandler<TSubscriber>(subscriber);
        }
    }
}
