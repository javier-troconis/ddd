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
using shared;
using Xunit;
using Xunit.Abstractions;

namespace idology.api.tests
{
    public class UnitTest1
    {
        private readonly ITestOutputHelper _output;

        public UnitTest1(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task Test1()
        {
            var callbackUri = "http://localhost:9999/webhook/";
            var syncReqs = Enumerable.Range(0, 5)
                .Select(x =>
                    new Dictionary<string, object>
                    {
                        ["request-timeout"] = 10000
                    });
            var asyncWithPollingReqs = Enumerable.Range(0, 5)
                .Select(x =>
                    new Dictionary<string, object>
                    {
                        ["request-timeout"] = 1
                    });
            var asyncWithCallbackReqs = Enumerable.Range(0, 5)
                .Select(x =>
                    new Dictionary<string, object>
                    {
                        ["request-timeout"] = 1,
                        ["callback-uri"] = callbackUri
                    });
            var client = new HttpClient();
            var server = new HttpListener();
            server.Prefixes.Add(callbackUri);
            server.Start();

            var result = await Task.WhenAll(
                new[]
                {
                    syncReqs,
                    asyncWithPollingReqs,
                    asyncWithCallbackReqs
                }
                .SelectMany(x => x)
                .AsParallel()
                .Select(
                    async (req, x) =>
                    {
                        var watch = Stopwatch.StartNew();
                        _output.WriteLine($"starting op #: {x}");
                        var y = await ProcessRequest(server, client, req);
                        watch.Stop();
                        _output.WriteLine($"finished op #: {x} ({y.Item1}). took: {watch.ElapsedMilliseconds} (ms)");
                        return y.Item2;
                    }));

            server.Stop();

            foreach (var i in result)
            {
                Assert.Equal(i.GetValues("command-correlation-id"), i.GetValues("event-correlation-id"));
            }
        }

        static async Task<Tuple<string, HttpHeaders>> ProcessRequest(HttpListener server, HttpClient client, IDictionary<string, object> req)
        {
            var httpRequestMessage = new HttpRequestMessage
            {
                RequestUri = new Uri("http://localhost:7071/identityverification"),
                Method = HttpMethod.Post
            };
            foreach (var h in req)
            {
                httpRequestMessage.Headers.Add(h.Key, h.Value.ToString());
            }
            
            var response = await client.SendAsync(httpRequestMessage);

            if (response.StatusCode == HttpStatusCode.Accepted && req.ContainsKey("callback-uri"))
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
                return new Tuple<string, HttpHeaders>("async callback", response2.Headers);
            }

            if (response.StatusCode == HttpStatusCode.Accepted && !req.ContainsKey("callback-uri"))
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
                return new Tuple<string, HttpHeaders>("async polling", response3.Headers);
            }

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return new Tuple<string, HttpHeaders>("sync", response.Headers);
            }

            throw new NotSupportedException();
        }


    }
}
