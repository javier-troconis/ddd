using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using contracts;
using shared;

namespace core
{
   

	public static class Commands
    {
        public static IEnumerable<object> StartApplicationV1()
        {
            yield return new ApplicationStartedV1();
        }

		public static IEnumerable<object> StartApplicationV2()
		{
			yield return new ApplicationStartedV2();
		}

		public static IEnumerable<object> StartApplicationV3()
		{
			yield return new ApplicationStartedV3();
		}

		public static IEnumerable<object> SubmitApplicationV1(SubmitApplicationState state, string submitter)
		{
			if (!state.ApplicationHasBeenStarted)
			{
				throw new Exception("application has not been started");
			}

			yield return new ApplicationSubmittedV1(submitter);
		}
	}
}
