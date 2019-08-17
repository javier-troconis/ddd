using eventstore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
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
            var syncReqs = Enumerable.Range(0, 100)
                .Select(x =>
                    new Dictionary<string, object>
                    {
                        ["requestTimeout"] = 10000,
                        ["callbackUri"] = string.Empty
                    });
            var asyncWithPollingReqs = Enumerable.Range(0, 100)
                .Select(x =>
                    new Dictionary<string, object>
                    {
                        ["requestTimeout"] = 1,
                        ["callbackUri"] = string.Empty
                    });
            var asyncWithCallbackReqs = Enumerable.Range(0, 100)
                .Select(x =>
                    new Dictionary<string, object>
                    {
                        ["requestTimeout"] = 1,
                        ["callbackUri"] = callbackUri
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
                        _output.WriteLine("starting req #: " + x);
                        var y = await ProcessRequest(server, client, req);
                        watch.Stop();
                        _output.WriteLine($"finished req ({y.Item1}) #: {x}. took: {watch.ElapsedMilliseconds} (ms).");
                        return y.Item2;
                    }));
            server.Stop();

            foreach (var i in result)
            {
                Assert.Equal(i["cmdCorrelationId"], i["evtCorrelationId"]);
            }
        }

        static async Task<Tuple<string, IDictionary<string, object>>> ProcessRequest(HttpListener server, HttpClient client, IDictionary<string, object> req)
        {
            var response = await client.PostAsync("http://localhost:7071/identityverification", new StringContent(req.ToJson()));

            if (response.StatusCode == HttpStatusCode.Accepted && req["callbackUri"] != null)
            {
                var serverContext = await server.GetContextAsync();
                var callbackRequest = serverContext.Request;
                string request1;
                using (var stream = callbackRequest.InputStream)
                {
                    var reader = new StreamReader(stream);
                    request1 = reader.ReadToEnd();
                }
                var response1 = serverContext.Response;
                response1.StatusCode = (int)HttpStatusCode.OK;
                response1.Close();
                var response2 = await client.GetAsync(request1);
                var content1 = await response2.Content.ReadAsStringAsync();
                return new Tuple<string, IDictionary<string, object>>("async w/ callback",
                    content1.ParseJson<IDictionary<string, object>>());
            }

            if (response.StatusCode == HttpStatusCode.Accepted && req["callbackUri"] == null)
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
                    await Task.Delay(50);
                }
                var response3 = await client.GetAsync(resultUri);
                var content1 = await response3.Content.ReadAsStringAsync();
                return new Tuple<string, IDictionary<string, object>>("async w/ polling",
                    content1.ParseJson<IDictionary<string, object>>());
            }

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();
                return new Tuple<string, IDictionary<string, object>>("sync",
                    content.ParseJson<IDictionary<string, object>>());
            }

            throw new NotSupportedException();
        }


    }
}
