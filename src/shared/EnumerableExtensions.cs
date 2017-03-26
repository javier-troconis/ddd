using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace shared
{
    public static class EnumerableExtensions
    {
		public static Task<bool> AnyAsync<T>(this IEnumerable<T> source, Func<T, Task<bool>> predicate)
		{
			return source.Aggregate(Task.FromResult(false), async (x, y) => await x || await predicate(y));
		}
	}
}
