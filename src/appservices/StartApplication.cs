using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core;
using EventStore.ClientAPI;
using infra;
using shared;

namespace appservices
{
    public class StartApplicationCommand
    {
        public Guid ApplicationId { get; set; }
    }


    public class StartApplicationCommandHandler : IMessageHandler<Message<StartApplicationCommand>, Task<Message<StartApplicationCommand>>>
    {
        private readonly IEventStore _eventStore;

        public StartApplicationCommandHandler(IEventStore eventStore)
        {
            _eventStore = eventStore;
        }

        public async Task<Message<StartApplicationCommand>> Handle(Message<StartApplicationCommand> message)
        {
            var streamName = "application-" + NamingConvention.Stream(message.Body.ApplicationId);
            var newChanges = StartApplication.Apply();
            await _eventStore.WriteEventsAsync(streamName, ExpectedVersion.NoStream, newChanges, (e, h) => h["topics"] = e.GetEventTopics());
            return message;
        }
    }
}
