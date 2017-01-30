using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace shared
{
    public class NewType<T> : IEquatable<NewType<T>>
    {
        public readonly T Value;

        protected NewType(T value)
        {
            Value.EnsureNotNull();
            Value = value;
        }

        public static implicit operator NewType<T>(T value)
        {
            return new NewType<T>(value);
        }

        public static implicit operator T(NewType<T> value)
        {
            return value.Value;
        }

        public bool Equals(NewType<T> other)
        {
            return !ReferenceEquals(other, null) && Value.Equals(other.Value);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as NewType<T>);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}
