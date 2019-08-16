using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using eventstore;
using Xunit;

namespace idology.api.tests
{
    public class UnitTest1
    {
        [Fact]
        public async Task Test1()
        {
            var syncReqs = Enumerable.Range(0, 10)
                .Select(x =>
                    new
                    {
                        requestTimeout = 100,
                        callbackUri = string.Empty
                    });
            var asyncWithoutCallbackReqs = Enumerable.Range(0, 10)
                .Select(x =>
                    new
                    {
                        requestTimeout = 1,
                        callbackUri = string.Empty
                    });
            var asyncWithCallbackReqs = Enumerable.Range(0, 10)
                .Select(x => 
                    new
                    {
                        requestTimeout = 1,
                        callbackUri = $"http://localhost:999{x}/webhook/"
                    });

            var reqs =
                    new[]
                    {
                        syncReqs,
                        asyncWithoutCallbackReqs,
                        asyncWithCallbackReqs
                    }
                    .SelectMany(x => x)
                    .Select(x => new Func<Task<IDictionary<string, object>>>(async () =>
                    {
                        var client = new HttpClient();
                        var response = await client.SendAsync(
                            new HttpRequestMessage(HttpMethod.Post, "http://localhost:7071/identityverification")
                            {
                                Content = new StringContent(x.ToJson())
                            });
                        switch (response.StatusCode)
                        {
                            case HttpStatusCode.Accepted when !string.IsNullOrEmpty(x.callbackUri):
                            {
                                var server = new HttpListener();
                                server.Prefixes.Add(x.callbackUri);
                                server.Start();
                                var context = server.GetContext();
                                var callbackRequest = context.Request;
                                string request1;
                                using (var stream = callbackRequest.InputStream)
                                {
                                    var reader = new StreamReader(stream);
                                    request1 = reader.ReadToEnd();
                                }
                                var response1 = context.Response;
                                response1.StatusCode = 200;
                                response1.Close();
                                server.Stop();
                                var response2 = await client.GetAsync(request1);
                                var content1 = await response2.Content.ReadAsStringAsync();
                                return content1.ParseJson<IDictionary<string, object>>();
                            }
                            case HttpStatusCode.Accepted when string.IsNullOrEmpty(x.callbackUri):
                            {
                                var queueUri = response.Headers.Location;
                                Uri resultUri;
                                do
                                {
                                    var response2 = await client.GetAsync(queueUri);
                                    resultUri = response2.Headers.Location;
                                } while (Equals(resultUri, default(Uri)));
                                var response3 = await client.GetAsync(resultUri);
                                var content1 = await response3.Content.ReadAsStringAsync();
                                return content1.ParseJson<IDictionary<string, object>>();
                            }
                            case HttpStatusCode.OK:
                            {
                                var content = await response.Content.ReadAsStringAsync();
                                return content.ParseJson<IDictionary<string, object>>();
                            }
                            default:
                                throw new NotSupportedException();
                        }
                    }));

            var result = await Task.WhenAll(reqs.AsParallel().Select(x => x()));
            foreach (var i in result)
            {
                Assert.Equal(i["cmdCorrelationId"], i["evtCorrelationId"]);
            }
        }
    }
}
