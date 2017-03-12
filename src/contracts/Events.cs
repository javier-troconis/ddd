using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using shared;

namespace contracts
{
    public interface IApplicationStartedV1 : IEvent
    {
    }

	public interface IApplicationStartedV2 : IApplicationStartedV1
	{
	}

	public interface IApplicationStartedV3 : IApplicationStartedV2
	{
	}

	public interface IApplicationSubmittedV1 : IEvent
    {

    }

}
