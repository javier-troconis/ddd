using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace shared
{
    public static class Execute
    {
        public static async Task<T> Until<T>(Func<int, Task<T>> operation, Func<T, bool> shouldStop, int maxIterations)
        {
            for (var iteration = 1; maxIterations >= iteration; ++iteration)
            {
                var result = await operation(iteration);
                if (shouldStop(result))
                {
                    return result;
                }
            }
            return default(T);
        }
    }
}
