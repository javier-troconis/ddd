using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace shared
{
    public class ValueTypeComparer<T> : IEqualityComparer<T>
    {
	    private class ValueTypeWrapper : ValueType<ValueTypeWrapper>
		{
			public T Value { get; }

			public ValueTypeWrapper(T value)
			{
				Value = value;
			}
		}

	    public bool Equals(T x, T y)
	    {
			return new ValueTypeWrapper(x).Equals(new ValueTypeWrapper(y));
		}

	    public int GetHashCode(T x)
	    {
			return new ValueTypeWrapper(x).GetHashCode();
		}
    }
}
