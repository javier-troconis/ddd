using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace shared
{
	public abstract class ValueType<T> : IEquatable<T> where T : ValueType<T>
	{
		public static bool operator ==(ValueType<T> x, ValueType<T> y)
		{
			if (ReferenceEquals(x, y))
			{
				return true;
			}
			return !ReferenceEquals(x, null) && x.Equals(y);
		}

		public static bool operator !=(ValueType<T> x, ValueType<T> y)
		{
			return !(x == y);
		}

		public bool Equals(T other)
		{
			return !ReferenceEquals(other, null) && GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as T);
		}

		public override int GetHashCode()
		{
			return GetEqualityComponents().Aggregate(default(int), (x, y) => Tuple.Create(x, y).GetHashCode());
		}

		protected virtual IEnumerable<object> GetEqualityComponents()
		{
			return GetType()
				.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
				.Select(x => x.GetValue(this));
		}
	}
}
