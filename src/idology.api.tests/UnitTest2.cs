using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using shared;
using Xunit;
using Xunit.Abstractions;

namespace idology.api.tests
{
    public class UnitTest2
    {
        private readonly ITestOutputHelper _output;

        public UnitTest2(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Test2()
        {
            Func<int, object> f = x =>
            {
                var y = new object();
                return y;
            };

            var mc = new MemoryCache(new MemoryCacheOptions());
            var mceo = new MemoryCacheEntryOptions();
            var fm = f.Memoize(mc, mceo);

            var sw = new Stopwatch();
            sw.Start();
            foreach (var x in Enumerable.Range(0, 100000).AsParallel())
            {
                Enumerable.Repeat(0, 100).ToList().ForEach(y => fm(x));
            }
            sw.Stop();
            _output.WriteLine(sw.ElapsedMilliseconds.ToString());
        }

        [Fact]
        public void Test3()
        {
            Func<int, object> f = x =>
            {
                var y = new object();
                _output.WriteLine(y.GetHashCode().ToString());
                return y;
            };

            var mc = new MemoryCache(new MemoryCacheOptions());
            var mceo = new MemoryCacheEntryOptions();
            var fm = f.Memoize(mc, mceo);
            var r = Enumerable.Repeat(0, 10).AsParallel().Select(fm);
            _output.WriteLine(string.Join(",", r.Select(x => x.GetHashCode())));
        }
    }
}
