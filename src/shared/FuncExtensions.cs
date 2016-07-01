using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace shared
{
    public static class FuncExtensions
    {
        public static Func<TIn, TOut1> ComposeForward<TIn, TOut, TOut1>(this Func<TIn, TOut> @from, Func<TOut, TOut1> to)
        {
            return x => to(@from(x));
        }

        public static Func<TIn1, TOut> ComposeBackward<TIn1, TIn, TOut>(this Func<TIn, TOut> to, Func<TIn1, TIn> @from)
        {
            return @from.ComposeForward(to);
        }
    }
}
