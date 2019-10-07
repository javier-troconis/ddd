using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using eventstore;
using shared;

namespace idology.api.messaging.host
{
    public class EchoService : IMessageHandler<object, Task<IEnumerable<Message<byte[]>>>>
    {
        private readonly string _messageName;

        public EchoService(string messageName)
        {
            _messageName = messageName;
        }

        public async Task<IEnumerable<Message<byte[]>>> Handle(object messageData)
        { 
            return await Task.FromResult(new []
            {
                new Message<byte[]>(_messageName, messageData.ToJsonBytes())
            });
        }
    }
}
