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
        public static IDictionary<TKey, TValue> Merge<TKey, TValue>(this IDictionary<TKey, TValue> x, IDictionary<TKey, TValue> y)
        {
            return x
                .Except(x.Join(y, z => z.Key, z => z.Key, (a, b) => a))
                .Concat(y)
                .ToDictionary(z => z.Key, z => z.Value);
        }
    }
}
