using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using shared;

namespace command.contracts
{
	[Topic]
	public interface IIdentityVerificationCompleted_V1
    {
        string TransactionId { get; }
        string Result { get; }
    }
}
