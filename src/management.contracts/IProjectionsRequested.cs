using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace management.contracts
{
    public interface IProjectionsRequested
    {
		string ServiceName { get; }
		string ProjectionName { get; }
	}
}
