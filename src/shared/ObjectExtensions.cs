using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace shared
{
    public static class ObjectExtensions
    {
        public static TOut PipeForward<TIn, TOut>(this TIn a, Func<TIn, TOut> b)
        {
            return b(a);
        }
    }
}
