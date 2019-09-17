using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using shared;

namespace idology.api.messaging.host
{
    public class EchoService : IMessageHandler<object, Task<object>>
    {
        public Task<object> Handle(object message)
        {
            return Task.FromResult(message);
        }
    }
}
