using System;
using System.Collections.Generic;
using System.Net.Http;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using shared;

namespace idology.azurefunction
{
    public interface ISendCommandService
    {
        Task<HttpResponseMessage> SendCommand(Guid correlationId, Guid commandId, Message<byte[]> command,
            string[] commandCompletionMessageTypes, ILogger logger, CancellationTokenSource cts, Uri resultBaseUri,
            Uri queueBaseUri, Uri callbackUri = null, IDictionary<string, object> metadata = null);
    }
}
