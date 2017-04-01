using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace shared
{
    public static class EnumerableExtensions
    {
		public static Task<bool> AnyAsync<T>(this IEnumerable<T> seq, Func<T, Task<bool>> predicate)
		{
            return seq.AnyAsync((x, y) => predicate(x));
		}

        public static Task<bool> AnyAsync<T>(this IEnumerable<T> seq, Func<T, int, Task<bool>> predicate)
        {
            return seq.Aggregate(Task.FromResult(new { Passed = false, Iteration = 0 }), 
                async (x, y) => 
            {
                var result = await x;
                return result.Passed ? result : new { Passed = await predicate(y, result.Iteration), Iteration = result.Iteration + 1 };
            }, 
                async x => 
            {
                var result = await x;
                return result.Passed;
            });
        }
    }
}
