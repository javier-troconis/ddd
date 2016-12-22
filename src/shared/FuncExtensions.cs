using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace shared
{
    public static class FuncExtensions
    {
        public static Func<T2> Partial<T1, T2>(this Func<T1, T2> f, T1 a)
        {
            return () => f(a);
        }

        public static Func<T2, T3> Partial<T1, T2, T3>(this Func<T1, T2, T3> f, T1 a)
        {
            return b => f(a, b);
        }

        public static Func<T2, T3, T4> Partial<T1, T2, T3, T4>(this Func<T1, T2, T3, T4> f, T1 a)
        {
            return (b, c) => f(a, b, c);
        }
        public static Func<T2, T3, T4, T5> Partial<T1, T2, T3, T4, T5>(this Func<T1, T2, T3, T4, T5> f, T1 a)
        {
            return (b, c, d) => f(a, b, c, d);
        }

        public static Func<T2, T3, T4, T5, T6> Partial<T1, T2, T3, T4, T5, T6>(this Func<T1, T2, T3, T4, T5, T6> f, T1 a)
        {
            return (b, c, d, e) => f(a, b, c, d, e);
        }

        public static Func<T1, T3> ComposeForward<T1, T2, T3>(this Func<T1, T2> f1, Func<T2, T3> f2)
        {
            return x => f2(f1(x));
        }

        public static Func<T1, T3> ComposeBackward<T1, T2, T3>(this Func<T2, T3> f1, Func<T1, T2> f2)
        {
            return f2.ComposeForward(f1);
        }
    }
}
