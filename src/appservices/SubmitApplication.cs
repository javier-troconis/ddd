using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using core;
using infra;
using shared;

namespace appservices
{
    public class SubmitApplicationCommand
    {
        public Guid ApplicationId { get; set; }
        public string Submitter { get; set; }
        public int Version { get; set; }
    }

    public class SubmitApplicationCommandHandler : IMessageHandler<Message<SubmitApplicationCommand>, Task<Message<SubmitApplicationCommand>>>
    {
        private readonly IEventStore _eventStore;

        public SubmitApplicationCommandHandler(IEventStore eventStore)
        {
            _eventStore = eventStore;
        }

        public async Task<Message<SubmitApplicationCommand>> Handle(Message<SubmitApplicationCommand> message)
        {
            var applicationId = "application-" + StreamNamingConvention.From(message.Body.ApplicationId);
            var currentChanges = await _eventStore.ReadEventsAsync(applicationId);
            var currentState = currentChanges.Aggregate(new WhenSubmittingApplicationState(), StreamStateFolder.Fold);
            var newChanges = SubmitApplication.Apply(currentState, message.Body.Submitter);
            await OptimisticEventWriter.WriteEventsAsync(StreamVersionConflictResolution.AlwaysCommit, _eventStore, applicationId, message.Body.Version, newChanges);
            return message;
        }
    }
}
