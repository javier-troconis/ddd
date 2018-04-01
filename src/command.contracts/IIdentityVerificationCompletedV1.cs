using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace command.contracts
{
    public interface IIdentityVerificationCompletedV1
    {
        string TransactionId { get; }
        string Result { get; }
    }
}
