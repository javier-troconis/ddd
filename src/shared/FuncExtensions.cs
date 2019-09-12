using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading;
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
            return a => f2(f1(a));
        }

        public static Func<T1, Task<T3>> ComposeForward<T1, T2, T3>(this Func<T1, Task<T2>> f1, Func<T2, Task<T3>> f2)
        {
            return f1.ComposeForward(f2.ToAsync());
        }

        public static Func<T1> Memoize<T1>(this Func<T1> f, IMemoryCache cache, MemoryCacheEntryOptions cacheEntryOptions)
        {
            var f1 = new Func<Unit, T1>(a => f()).Memoize(cache, cacheEntryOptions);
            return () => f1(Unit.Value);
        }

        public static Func<T1, T2> Memoize<T1, T2>(this Func<T1, T2> f, IMemoryCache cache, MemoryCacheEntryOptions cacheEntryOptions, Func<T1, object> getCacheEntryKey = null)
        {
            var l = new object();
            return a =>
            {
                var key = getCacheEntryKey == null ? a : getCacheEntryKey(a);
                if (cache.TryGetValue(key, out T2 value))
                {
                    return value;
                }
                lock (l)
                {
                    return cache.TryGetValue(key, out value) ? value : cache.Set(key, f(a), cacheEntryOptions);
                }
            };
        }

        public static Func<T1, T2, T3> Memoize<T1, T2, T3>(this Func<T1, T2, T3> f, IMemoryCache cache, MemoryCacheEntryOptions cacheEntryOptions, Func<Tuple<T1, T2>, object> getCacheEntryKey = null)
        {
            return f
                .Tuplify()
                .Memoize(cache, cacheEntryOptions)
                .Detuplify();
        }

        public static Func<Tuple<T1, T2>, T3> Tuplify<T1, T2, T3>(this Func<T1, T2, T3> f)
        {
            return a => f(a.Item1, a.Item2);
        }

        public static Func<T1, T2, T3> Detuplify<T1, T2, T3>(this Func<Tuple<T1, T2>, T3> f)
        {
            return (a, b) => f(new Tuple<T1, T2>(a, b));
        }

        public static Func<Task<T1>, Task<T2>> ToAsync<T1, T2>(this Func<T1, Task<T2>> f)
        {
            return async a => await f(await a);
        }

        public static Func<Task<T1>, Task<T2>> ToAsync<T1, T2>(this Func<T1, T2> f)
        {
            return async a => f(await a);
        }

        public static Func<T1, Task<T2>> Delay<T1, T2>(this Func<T1, T2> f, TimeSpan timeout, CancellationToken ct)
        {
            return a =>
                Task.Delay(timeout, ct)
                    .ContinueWith(t => f(a), TaskContinuationOptions.NotOnCanceled);
        }

        public static Func<T1, Task<T2>> Delay<T1, T2>(this Func<T1, Task<T2>> f, TimeSpan timeout, CancellationToken ct)
        {
            return a =>
                Task.Delay(timeout, ct)
                    .ContinueWith(t => f(a), TaskContinuationOptions.NotOnCanceled)
                        .Unwrap();
        }

        public static Func<T1, Func<T2, T3>> Curry<T1, T2, T3>(this Func<T1, T2, T3> f)
        {
            return a => b => f(a, b);
        }

        public static Func<T1, T2, T3> Uncurry<T1, T2, T3>(this Func<T1, Func<T2, T3>> f)
        {
            return (a, b) => f(a)(b);
        }
    }
}
