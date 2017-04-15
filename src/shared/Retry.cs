using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace shared
{
    public static class Retry
    {
        public static async Task<T> RetryUntil<T>(Func<int, Task<T>> function, Func<T, bool> predicate, int maxIterations)
        {
            T result = default(T);
            for (var iteration = 1; maxIterations >= iteration; ++iteration)
            {
                result = await function(iteration);
                if (predicate(result))
                {
                    break;
                }
            }
            return result;
        }
    }
}
