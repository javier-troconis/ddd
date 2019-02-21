using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using shared;

namespace command.contracts
{
	[Topic]
    public interface IApplicationStarted_V3
    {
        string ApplicantSsn { get; }
	    string ApplicantEmail { get; }
	    string ApplicantName { get; }
	}

}
