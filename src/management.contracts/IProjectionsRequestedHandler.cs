using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using shared;

namespace management.contracts
{
    public interface IProjectionsRequestedHandler : IMessageHandler<IRecordedEvent<IProjectionsRequested>, Task>
    {

    }
}
