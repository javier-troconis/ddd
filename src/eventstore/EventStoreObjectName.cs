using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eventstore
{
    public struct EventStoreObjectName
    {
        public readonly string Value;

        private EventStoreObjectName(string value)
        {
	        Value = value
		        .Replace('.', '_')
		        .Replace("+", "_");
        }

		public static implicit operator string(EventStoreObjectName value)
        {
            return value.Value;
        }

        public static implicit operator EventStoreObjectName(string value)
        {
            return new EventStoreObjectName(value);
        }

	    public static implicit operator EventStoreObjectName(Type value)
	    {
		    return new EventStoreObjectName(value.FullName);
	    }
	}
}
