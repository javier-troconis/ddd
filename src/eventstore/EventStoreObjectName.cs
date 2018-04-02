using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eventstore
{
    public struct EventStoreObjectName
    {
        private readonly string _value;

        private EventStoreObjectName(string value)
        {
            _value = value.GetEventStoreObjectName();
        }

        public static implicit operator string(EventStoreObjectName value)
        {
            return value._value;
        }

        public static implicit operator EventStoreObjectName(string value)
        {
            return new EventStoreObjectName(value);
        }

	    public static implicit operator EventStoreObjectName(Type value)
	    {
		    return new EventStoreObjectName(value.GetEventStoreObjectName());
	    }
	}
}
