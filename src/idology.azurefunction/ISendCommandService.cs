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
        public Command(Guid commandId, string commandName, byte[] commandData)
        {
            CommandId = commandId;
            CommandName = commandName;
            CommandData = commandData;
        }

        public Guid CommandId { get; }
        public string CommandName { get; }
        public byte[] CommandData { get; }
    }

    public interface ISendCommandService
    {
        Task<HttpResponseMessage> SendCommand(Guid correlationId, Command command,
            string[] commandCompletionMessageTypes, ILogger logger, CancellationTokenSource cts, Uri resultBaseUri,
            Uri queueBaseUri, Uri callbackUri = null);
    }
}
