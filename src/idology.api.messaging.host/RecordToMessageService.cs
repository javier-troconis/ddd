using System;
using System.Collections.Generic;
using System.Text;
using eventstore;
using shared;

namespace idology.api.messaging.host
{
    public class RecordToMessageService : IMessageHandler<object, IEnumerable<Message>>
    {
        private readonly string _messageName;

        public RecordToMessageService(string messageName)
        {
            _messageName = messageName;
        }

        public IEnumerable<Message> Handle(object message)
        {
            yield return new Message(_messageName, message.ToJsonBytes());
        }
    }
}
