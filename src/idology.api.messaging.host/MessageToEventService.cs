using System;
using System.Collections.Generic;
using System.Text;
using shared;

namespace idology.api.messaging.host
{
    public class MessageToEventService : IMessageHandler<object, IEnumerable<Event>>
    {
        private readonly string _eventName;

        public MessageToEventService(string eventName)
        {
            _eventName = eventName;
        }

        public IEnumerable<Event> Handle(object message)
        {
            yield return new Event(_eventName, message);
        }
    }
}
