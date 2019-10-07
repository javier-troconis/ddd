using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using eventstore;
using shared;

namespace idology.api.messaging.host
{
    public class ObjectToMessageService : IMessageHandler<object, Task<IEnumerable<Message<byte[]>>>>
    {
        private readonly string _messageName;

        public ObjectToMessageService(string messageName)
        {
            _messageName = messageName;
        }

        public async Task<IEnumerable<Message<byte[]>>> Handle(object message)
        { 
            return await Task.FromResult(new []
            {
                new Message<byte[]>(_messageName, message.ToJsonBytes())
            });
        }
    }
}
