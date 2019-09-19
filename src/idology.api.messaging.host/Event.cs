using System;
using System.Collections.Generic;
using System.Text;

namespace idology.api.messaging.host
{
    public struct Event
    {
        public string Name { get; }
        public object Data { get; }

        public Event(string name, object data)
        {
            Name = name;
            Data = data;
        }
    }
}
