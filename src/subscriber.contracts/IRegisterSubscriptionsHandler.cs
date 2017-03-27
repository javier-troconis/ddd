using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using shared;

namespace subscriber.contracts
{
    public interface IRegisterSubscriptionsHandler : IMessageHandler<IRecordedEvent<ISubscriptionsRegistrationRequested>, Task>
    {

    }
}
