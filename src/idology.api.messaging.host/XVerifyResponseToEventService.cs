using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using eventstore;
using EventStore.ClientAPI;
using shared;

namespace idology.api.messaging.host
{
    public class XVerifyResponseToEventService : IMessageHandler<XVerifyIdentityResponse, IEnumerable<Message<byte[]>>>
    {
        public IEnumerable<Message<byte[]>> Handle(XVerifyIdentityResponse message)
        {
            if (message.Decision == XDecision.Passed)
            {
                yield return new Message<byte[]>("identityverificationpassed", message.ToJsonBytes());
            }
            else
            {
                yield return new Message<byte[]>("identityverificationfailed", message.ToJsonBytes());
            }
        }
    }
}
