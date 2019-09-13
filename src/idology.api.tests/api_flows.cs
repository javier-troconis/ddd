using eventstore;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Dynamitey.DynamicObjects;
using shared;
using Xunit;
using Xunit.Abstractions;

namespace idology.api.tests
{
    public class api_flows
    {
        private readonly ITestOutputHelper _output;

        public api_flows(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [MemberData(nameof(TestData), MemberType = typeof(api_flows))]
        public async Task api_flows_should_complete(string flowType, IEnumerable<IDictionary<string, object>> requestHeaders)
        {
            var responses = await SendRequests(_output, requestHeaders);
            foreach (var i in responses)
            {
                Assert.Equal(i.Item1, flowType);
                Assert.Equal(i.Item2.Headers.GetValues("command-correlation-id"), i.Item2.Headers.GetValues("event-correlation-id"));
            }
        }

        public static IEnumerable<object[]> TestData => new[]
        {
            new object[]
            {
                "sync",
                Enumerable.Range(0, 5)
                    .Select(x =>
                        new Dictionary<string, object>
                        {
                            ["request-timeout"] = 10000
                        })
            },
            new object[]
            {
                "async polling",
                Enumerable.Range(0, 5)
                    .Select(x =>
                        new Dictionary<string, object>
                        {
                            ["request-timeout"] = 1
                        })
            },
            new object[]
            {
                "async callback",
                Enumerable.Range(0, 5)
                    .Select(x =>
                        new Dictionary<string, object>
                        {
                            ["request-timeout"] = 1,
                            ["callback-uri"] = "http://localhost:9999/webhook/"
                        })
            }
        };

        static async Task<IEnumerable<Tuple<string, HttpResponseMessage>>> SendRequests(ITestOutputHelper output, IEnumerable<IDictionary<string, object>> requestHeaders)
        {
            var callbackUri = "http://localhost:9999/webhook/";
            var client = new HttpClient();
            var server = new HttpListener();
            server.Prefixes.Add(callbackUri);
            server.Start();
            var result = await Task.WhenAll(
                requestHeaders
                    .AsParallel()
                    .Select(
                        async (req, x) =>
                        {
                            var watch = Stopwatch.StartNew();
                            output.WriteLine($"req #: {x}");
                            var y = await SendRequest(server, client, req);
                            watch.Stop();
                            output.WriteLine($"res #: {x} ({y.Item1}). took: {watch.ElapsedMilliseconds} (ms)");
                            return y;
                        }));
            server.Stop();
            return result;
        }

        static async Task<Tuple<string, HttpResponseMessage>> SendRequest(HttpListener server, HttpClient client, IDictionary<string, object> requestHeaders)
        {
            var httpRequestMessage = new HttpRequestMessage
            {
                RequestUri = new Uri("http://localhost:7071/identityverification"),
                Method = HttpMethod.Post
            };
            foreach (var h in requestHeaders)
            {
                httpRequestMessage.Headers.Add(h.Key, h.Value.ToString());
            }

            var response = await client.SendAsync(httpRequestMessage);

            if (response.StatusCode == HttpStatusCode.Accepted && requestHeaders.ContainsKey("callback-uri"))
            {
                var context = await server.GetContextAsync();
                var callbackRequest = context.Request;
                string resultUri;
                using (var stream = callbackRequest.InputStream)
                {
                    var reader = new StreamReader(stream);
                    resultUri = reader.ReadToEnd();
                }
                var response1 = context.Response;
                response1.StatusCode = (int)HttpStatusCode.OK;
                response1.Close();
                var response2 = await client.GetAsync(resultUri);
                return new Tuple<string, HttpResponseMessage>("async callback", response2);
            }

            if (response.StatusCode == HttpStatusCode.Accepted && !requestHeaders.ContainsKey("callback-uri"))
            {
                var queueUri = response.Headers.Location;
                Uri resultUri;
                while (true)
                {
                    var response2 = await client.GetAsync(queueUri);
                    if (!Equals(resultUri = response2.Headers.Location, null))
                    {
                        break;
                    }
                    await Task.Delay(100);
                }
                var response3 = await client.GetAsync(resultUri);
                return new Tuple<string, HttpResponseMessage>("async polling", response3);
            }

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return new Tuple<string, HttpResponseMessage>("sync", response);
            }

            throw new NotSupportedException();
        }


    }
}
