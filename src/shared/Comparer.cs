using System;
using System.Collections.Generic;
using System.Text;

namespace shared
{
    public static class Comparer
    {
        public static IComparer<T> Create<T>(Func<T, T, int> comparer)
        {
            return new X<T>(comparer);
        }

        private class X<T> : IComparer<T>
        {
            private readonly Func<T, T, int> _compare;

            public X(Func<T, T, int> compare)
            {
                _compare = compare;
            }


            public int Compare(T x, T y)
            {
                return _compare(x, y);
            }
        }
    }
}
