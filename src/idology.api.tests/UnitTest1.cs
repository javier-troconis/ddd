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
            var responseData = new ConcurrentBag<IDictionary<string, string>>();
           
            var n = Enumerable.Range(0, 10)
                .Select(x => new Func<Task>(async () =>
                    {
                        var client = new HttpClient();
                        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:7071/identityverification")
                        {
                            Content = new StringContent(new
                            {
                                requestTimeout = 15,
                                callbackUri = "http://localhost:7071/webhook"
                            }.ToJson())
                        };
                        var response = await client.SendAsync(request);
                        var content = await response.Content.ReadAsStringAsync();
                        var data = content.ParseJson<IDictionary<string, string>>();
                        responseData.Add(data);
                    }));

            var processor = new ActionBlock<Func<Task>>(x => x(), new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount });
            await Task.WhenAll(n.Select(processor.SendAsync));
            processor.Complete();
            await processor.Completion;

            foreach (var s in responseData)
            {
                Assert.Equal(s["cmdCorrelationId"], s["evtCorrelationId"]);
            }
        }
    }
}
