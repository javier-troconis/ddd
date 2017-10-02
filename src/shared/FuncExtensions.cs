using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Caching.Memory;

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

		public static Func<T1, Task<T3>> ComposeForward<T1, T2, T3>(this Func<T1, Task<T2>> f1, Func<T2, Task<T3>> f2)
		{
			return f1.ComposeForward(f2.ToAsyncInput());
		}

		public static Func<T1, T3> ComposeBackward<T1, T2, T3>(this Func<T2, T3> f1, Func<T1, T2> f2)
		{
			return f2.ComposeForward(f1);
		}

		public static Func<T1, Task<T3>> ComposeBackward<T1, T2, T3>(this Func<T2, Task<T3>> f1, Func<T1, Task<T2>> f2)
		{
			return f2.ComposeForward(f1);
		}

		public static Func<T1, T2> Memoize<T1, T2>(this Func<T1, T2> f, IMemoryCache cache, MemoryCacheEntryOptions memoryCacheEntryOptions, Func<T1, object> getEntryKey = null)
		{
			return x => cache.GetOrCreate(
				getEntryKey == null ? x : getEntryKey(x), 
				z =>
				{
					z.SetOptions(memoryCacheEntryOptions);
					return new Lazy<T2>(() => f(x), LazyThreadSafetyMode.ExecutionAndPublication);
				}).Value;
		}

		public static Func<T1, T2, T3> Memoize<T1, T2, T3>(this Func<T1, T2, T3> f, IMemoryCache cache, MemoryCacheEntryOptions memoryCacheEntryOptions, Func<T1, T2, object> getEntryKey = null)
		{
			var f1 = new Func<Tuple<T1, T2>, T3>(x => f(x.Item1, x.Item2))
				.Memoize(
					cache, 
					memoryCacheEntryOptions, 
					getEntryKey == null ? null : new Func<Tuple<T1, T2>, object>(entry => getEntryKey(entry.Item1, entry.Item2)));
			return (x, y) => f1(new Tuple<T1, T2>(x, y));
		}

		public static Func<T1, T2, T3, T4> Memoize<T1, T2, T3, T4>(this Func<T1, T2, T3, T4> f, IMemoryCache cache, MemoryCacheEntryOptions memoryCacheEntryOptions, Func<T1, T2, T3, object> getEntryKey = null)
		{
			var f1 = new Func<Tuple<T1, T2, T3>, T4>(x => f(x.Item1, x.Item2, x.Item3))
				.Memoize(
					cache, 
					memoryCacheEntryOptions,
					getEntryKey == null ? null : new Func<Tuple<T1, T2, T3>, object>(entry => getEntryKey(entry.Item1, entry.Item2, entry.Item3)));
			return (x, y, z) => f1(new Tuple<T1, T2, T3>(x, y, z));
		}

		public static Func<Task<T1>, Task<T2>> ToAsyncInput<T1, T2>(this Func<T1, Task<T2>> f)
		{
			return async x => await f(await x);
		}

		public static Func<Task<T1>, Task<T2>> ToAsyncInput<T1, T2>(this Func<T1, T2> f)
		{
			return async x => f(await x);
		}
    }
}
