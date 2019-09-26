using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using eventstore;
using EventStore.ClientAPI;
using shared;

namespace idology.api.messaging.host
{
    public class XVerifyResponseToEventService : IMessageHandler<XVerifyIdentityResponse, IEnumerable<Message>>
    {
        public IEnumerable<Message> Handle(XVerifyIdentityResponse message)
        {
            if (message.Decision == XDecision.Passed)
            {
                yield return new Message("identityverificationpassed", message.ToJsonBytes());
            }
            else
            {
                yield return new Message("identityverificationfailed", message.ToJsonBytes());
            }
        }
    }
}
