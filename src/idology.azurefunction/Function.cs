using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using EventStore.ClientAPI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using shared;

namespace idology.azurefunction
{
	public static class Function
	{
	    [FunctionName(nameof(EvaluateOfacCompliance))]
	    public static async Task<HttpResponseMessage> EvaluateOfacCompliance(CancellationToken ct, [HttpTrigger(AuthorizationLevel.Function, "post", Route = "api/v1/ofaccompliance")] HttpRequestMessage request, ExecutionContext ctx, Lazy<Task<Func<Predicate<ResolvedEvent>, CancellationToken, Task<ResolvedEvent>>>> deferredReceiveEvent, ILogger logger)
	    {
            var receiveEvent = await deferredReceiveEvent.Value;
	        var receiveEventTask = receiveEvent(x => x.IsResolved, ct);
            return new HttpResponseMessage(HttpStatusCode.Accepted);
	    }
    }
}
