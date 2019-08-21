using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eventstore
{
    public struct StreamId
    {
        public readonly string Value;

        private StreamId(string value)
        {
            Value = value.Substring(value.IndexOf("-", StringComparison.InvariantCultureIgnoreCase) + 1);
        }

        public static implicit operator StreamId(string value)
        {
            return new StreamId(value);
        }

        public static implicit operator string(StreamId value)
        {
            return value.Value;
        }
    }
}
