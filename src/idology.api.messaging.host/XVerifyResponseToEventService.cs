using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using eventstore;
using EventStore.ClientAPI;
using shared;

namespace idology.api.messaging.host
{
    public class XVerifyResponseToEventService : IMessageHandler<XVerifyIdentityResponse, IEnumerable<Event>>
    {
        public IEnumerable<Event> Handle(XVerifyIdentityResponse message)
        {
            yield return new Event("verifyidentitysucceeded", message);
        }
    }
}
