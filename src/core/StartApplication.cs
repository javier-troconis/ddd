using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using contracts;
using shared;

namespace core
{
    public struct ApplicationStartedV1 : IApplicationStartedV1
    {
	   
    }

	public struct ApplicationStartedV2 : IApplicationStartedV2
	{
		
	}

	public struct ApplicationStartedV3 : IApplicationStartedV3
	{
	
	}

	public static class StartApplication
    {
        public static IEnumerable<IEvent> ApplyApplicationStartedV1()
        {
            yield return new ApplicationStartedV1();
        }

		public static IEnumerable<IEvent> ApplyApplicationStartedV2()
		{
			yield return new ApplicationStartedV2();
		}

		public static IEnumerable<IEvent> ApplyApplicationStartedV3()
		{
			yield return new ApplicationStartedV3();
		}
	}
}
