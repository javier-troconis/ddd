using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace shared
{
	public abstract class Value<T> : IEquatable<T> where T : Value<T>
	{
		public static bool operator ==(Value<T> x, Value<T> y)
		{
			if (ReferenceEquals(x, y))
			{
				return true;
			}
			return !ReferenceEquals(x, null) && x.Equals(y);
		}

		public static bool operator !=(Value<T> x, Value<T> y)
		{
			return !(x == y);
		}

		public bool Equals(T other)
		{
			return !ReferenceEquals(other, null) && GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
		}

		public override bool Equals(object other)
		{
			return Equals(other as T);
		}

		public override int GetHashCode()
		{
			return GetEqualityComponents().Aggregate(0, (x, y) => Tuple.Create(x, y).GetHashCode());
		}

		public virtual IEnumerable<object> GetEqualityComponents()
		{
			return GetType()
				.GetFields(BindingFlags.Instance | BindingFlags.Public)
				.Select(x => x.GetValue(this));
		}
	}
}
