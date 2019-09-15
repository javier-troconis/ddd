using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using System.Linq;
using System.Collections.Concurrent;
using Xunit.Abstractions;
using shared;

namespace idology.api.tests
{
    /*
    class Producer
    {
        private readonly ConcurrentDictionary<object, Tuple<Predicate<int>, TaskCompletionSource<int>>> _consumers = new ConcurrentDictionary<object, Tuple<Predicate<int>, TaskCompletionSource<int>>>();
    
        public void Produce(int message)
        {
            foreach(var c in _consumers.ToArray())
            {
                if (c.Value.Item1(message))
                {
                    c.Value.Item2.TrySetResult(message);
                }
            }
        }

        public Func<CancellationToken, Task<int>> CreateConsumer(Predicate<int> filter)
        {
            return async cts => 
            {
                var key = new object();
                var tcs = new TaskCompletionSource<int>();
                _consumers.TryAdd(key, new Tuple<Predicate<int>, TaskCompletionSource<int>>(filter, tcs));
                try
                {
                    return await Task.Run(() => tcs.Task, cts);
                }
                catch (TaskCanceledException)
                {
                    tcs.TrySetCanceled();
                    throw;
                }
                finally
                {
                    _consumers.TryRemove(key, out _);
                }
            };
        }
    }
    */


    class Producer<T>
    {
        private readonly ConcurrentDictionary<TaskCompletionSource<T>, Predicate<T>> _consumers = new ConcurrentDictionary<TaskCompletionSource<T>, Predicate<T>>();

        public void Produce(T message)
        {
            foreach (var consumer in _consumers)
            {
                if (consumer.Value(message))
                {
                    consumer.Key.TrySetResult(message);
                }
            }
        }

        public Func<CancellationToken, Task<T>> CreateConsumer(Predicate<T> filter)
        {
            return async cts =>
            {
                var key = new TaskCompletionSource<T>();
                _consumers.TryAdd(key, filter);
                try
                {
                    return await key.Task.WithCancellation(cts);
                }
                catch (TaskCanceledException)
                {
                    key.TrySetCanceled();
                    throw;
                }
                finally
                {
                    _consumers.TryRemove(key, out _);
                }
            };
        }
    }

    public class Class1
    {
        private readonly ITestOutputHelper _output;

        public Class1(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task Test5()
        {
            var rnd = new Random();
            var p = new Producer<int>();
            Func<Task> produce = () =>
            {
                
                return Task.WhenAll(Enumerable.Range(1, 100).AsParallel().Select(async x =>
                {
                    p.Produce(x);
                    await Task.Delay(rnd.Next(0, 100));
                }));
            };
            var consumers = Enumerable.Range(1, 100)
                .AsParallel()
                .Select(x =>
                {
                    return p.CreateConsumer(x1 => x == x1);
                });
            var consume = Task.WhenAll(consumers.Select( (x,i) =>
                {
                    return Task.WhenAll(Enumerable.Repeat(0, 1).AsParallel().Select(async b => 
                    {
                    try
                    {
                        var v = await x(new CancellationTokenSource(rnd.Next(0, 100)).Token);
                        _output.WriteLine("received: " + v);
                        return v;
                    }
                    catch (TaskCanceledException)
                    {
                        _output.WriteLine("canceled: " + i);
                    }
                    return 0;
                    
                    }));
                    
                }));
           
            await Task.WhenAll(consume, produce());
        }
    }
}
