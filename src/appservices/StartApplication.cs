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


    public class StartApplicationCommandHandler : IMessageHandler<StartApplicationCommand, IEnumerable<IEvent>>
    {
        public IEnumerable<IEvent> Handle(StartApplicationCommand message)
        {
            return StartApplication.Apply();
        }
    }
}
