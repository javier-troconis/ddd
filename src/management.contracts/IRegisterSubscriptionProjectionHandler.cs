using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using shared;

namespace management.contracts
{
    public interface IRegisterSubscriptionProjectionHandler : IMessageHandler<IRecordedEvent<IRegisterSubscriptionProjection>, Task>
    {

    }
}
