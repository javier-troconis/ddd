using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace command
{
    public struct Ssn
    {
        private Ssn(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public static implicit operator string(Ssn value)
        {
            return value.Value;
        }

        public static implicit operator Ssn(string value)
        {
            return new Ssn(value);
        }
    }
}
