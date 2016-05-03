using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using infra;
using shared;

namespace host
{
    public class SubmitApplicationCommand
    {
        public Guid ApplicationId { get; set; }
    }

    public class SubmitApplicationCommandResult
    {
        public Guid ApplicationId { get; set; }
    }

    public class SubmitApplicationCommandHandler : IMessageHandler<SubmitApplicationCommand, SubmitApplicationCommandResult>
    {
        public SubmitApplicationCommandHandler(IEventStore eventStore)
        {

        }

        public SubmitApplicationCommandResult Handle(SubmitApplicationCommand message)
        {
            throw new NotImplementedException();
        }
    }
}
