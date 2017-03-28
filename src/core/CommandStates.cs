using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using shared;

namespace core
{
	public struct SubmitApplicationState :
		 IMessageHandler<ApplicationStartedV1, SubmitApplicationState>
	{
		public readonly bool ApplicationHasBeenStarted;

		public SubmitApplicationState(bool applicationHasBeenStarted)
		{
			ApplicationHasBeenStarted = applicationHasBeenStarted;
		}

		public SubmitApplicationState Handle(ApplicationStartedV1 message)
		{
			return new SubmitApplicationState(true);
		}
	}
}
