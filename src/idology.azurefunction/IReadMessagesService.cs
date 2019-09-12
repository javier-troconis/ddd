using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace idology.azurefunction
{
    public interface IReadMessagesService<TMessage>
    {
        Task<IEnumerable<TMessage>> ReadMessages(string streamName, ILogger logger);
    }
}
