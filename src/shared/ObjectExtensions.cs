using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace shared
{
    public static class ObjectExtensions
    {
        public static TOut PipeForward<TIn, TOut>(this TIn x, Func<TIn, TOut> f)
        {
            return f(x);
        }
    }

   
}
