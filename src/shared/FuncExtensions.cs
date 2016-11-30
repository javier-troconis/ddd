using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace shared
{
    public static class FuncExtensions
    {
        public static Func<TIn1, TOut2> ComposeForward<TIn1, TOut1, TOut2>(this Func<TIn1, TOut1> a, Func<TOut1, TOut2> b)
        {
            return x => b(a(x));
        }

        public static Func<TIn2, TOut1> ComposeBackward<TIn2, TOut2, TOut1>(this Func<TOut2, TOut1> a, Func<TIn2, TOut2> b)
        {
            return b.ComposeForward(a);
        }
    }
}
