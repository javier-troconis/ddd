using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace idology.azurefunction
{
    public struct Command
    {
        public Command(string commandName, byte[] commandData)
        {
            CommandName = commandName;
            CommandData = commandData;
        }

        public string CommandName { get; }
        public byte[] CommandData { get; }
    }

    public interface ISendCommandService
    {
        Task<HttpResponseMessage> SendCommand(Guid correlationId, Guid commandId, Command command,
            string[] commandCompletionMessageTypes, ILogger logger, CancellationTokenSource cts, Uri resultBaseUri,
            Uri queueBaseUri, Uri callbackUri = null, IDictionary<string, object> metadata = null);
    }
}
