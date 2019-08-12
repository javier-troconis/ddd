using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using eventstore;
using Xunit;

namespace idology.api.tests
{
    public class UnitTest1
    {
        class Res
        {
            public string CommandCorrelationId { get; set; }
            public string EventCorrelationId { get; set; }
        }

        [Fact]
        public async Task Test1()
        {
            var b = new ConcurrentBag<Dictionary<string, string>>();
           
            var n = Enumerable.Range(0, 5)
                .Select(x => new Func<Task>(async () =>
                    {
                        var client = new HttpClient();
                        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "http://localhost:7071/x"));
                        var content = await response.Content.ReadAsStringAsync();
                        var bn = content.ParseJson<Dictionary<string, string>>();
                        b.Add(bn);
                    }));

            var processor = new ActionBlock<Func<Task>>(x => x(), new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount });
            await Task.WhenAll(n.Select(processor.SendAsync));
            processor.Complete();
            await processor.Completion;

            foreach (var s in b)
            {
                Assert.Equal(s["cmdCorrelationId"], s["evtCorrelationId"]);
            }
        }
    }
}
