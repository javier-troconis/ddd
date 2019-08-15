using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
            var result = new ConcurrentBag<IDictionary<string, object>>();
            var processor = new ActionBlock<int>(async x =>
            {
                var callbackUri = $"http://localhost:999{x}/webhook/";
                var client = new HttpClient();
                var response = await client.SendAsync(
                    new HttpRequestMessage(HttpMethod.Post, "http://localhost:7071/identityverification")
                    {
                        Content = new StringContent(new
                        {
                            requestTimeout = 1,
                            callbackUri = callbackUri
                        }.ToJson())
                    });
                if (Equals(response.StatusCode, HttpStatusCode.OK))
                {
                    var content = await response.Content.ReadAsStringAsync();
                    result.Add(content.ParseJson<IDictionary<string, object>>());
                }
                else
                {
                    var server = new HttpListener();
                    server.Prefixes.Add(callbackUri);
                    server.Start();
                    var context = server.GetContext();
                    var callbackRequest = context.Request;
                    string resultUri;
                    using (var stream = callbackRequest.InputStream)
                    {
                        var reader = new StreamReader(stream);
                        resultUri = reader.ReadToEnd();
                    }
                    var response1 = context.Response;
                    response1.StatusCode = 200;
                    response1.Close();
                    server.Stop();
                    var response2 = await client.GetAsync(resultUri);
                    var content1 = await response2.Content.ReadAsStringAsync();
                    result.Add(content1.ParseJson<IDictionary<string, object>>());
                }

            }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount });

            var seq = Enumerable.Range(0, 1);
            await Task.WhenAll(seq.Select(processor.SendAsync));
            processor.Complete();
            await processor.Completion;

            foreach (var item in result)
            {
                Assert.Equal(item["cmdCorrelationId"], item["evtCorrelationId"]);
            }
        }
    }
}
