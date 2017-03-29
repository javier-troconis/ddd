using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using management.contracts;

namespace management
{
    public struct ProjectionsRequested : IProjectionsRequested
    {
	    public ProjectionsRequested(string serviceName, string projectionName)
	    {
		    ServiceName = serviceName;
		    ProjectionName = projectionName;
	    }

	    public string ServiceName { get; }
	    public string ProjectionName { get; }
    }
}
