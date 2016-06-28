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
    // This project can output the Class library as a NuGet Package.
    // To enable this option, right-click on the project and select the Properties menu item. In the Build tab select "Produce outputs on build".
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
            var applicationId = "application-" + StreamNamingConvention.From(message.Body.ApplicationId);
            var newChanges = StartApplication.Apply();
            await _eventStore.WriteEventsAsync(applicationId, ExpectedVersion.NoStream, newChanges);
            return message;
        }
    }



}
