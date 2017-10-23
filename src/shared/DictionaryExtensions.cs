using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shared
{
    public static class DictionaryExtensions
    {
		private class MyClass<TKey, TValue> : IEqualityComparer<KeyValuePair<TKey, TValue>>
		{
			public bool Equals(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y)
			{
				return Equals(x.Key, y.Key);
			}

			public int GetHashCode(KeyValuePair<TKey, TValue> obj)
			{
				return obj.Key.GetHashCode();
			}
		}

	    public static IDictionary<TKey, TValue> Merge<TKey, TValue>(this IDictionary<TKey, TValue> x, IDictionary<TKey, TValue> y)
	    {
		    return x
				.Concat(y)
			    .Distinct(new MyClass<TKey, TValue>())
			    .ToDictionary(z => z.Key, z => z.Value);
	    }
    }
}
