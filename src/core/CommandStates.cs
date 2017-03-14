using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using contracts;

using shared;

namespace core
{
	public struct SubmitApplicationState :
		 IMessageHandler<ApplicationStartedV1, SubmitApplicationState>,
		 IMessageHandler<ApplicationSubmittedV1, SubmitApplicationState>
	{
		public readonly bool ApplicationHasBeenStarted;
		public readonly bool ApplicationHasBeenSubmitted;

		public SubmitApplicationState(bool applicationHasBeenStarted, bool applicationHasBeenSubmitted)
		{
			ApplicationHasBeenStarted = applicationHasBeenStarted;
			ApplicationHasBeenSubmitted = applicationHasBeenSubmitted;
		}

		public SubmitApplicationState Handle(ApplicationSubmittedV1 message)
		{
			return new SubmitApplicationState(ApplicationHasBeenStarted, true);
		}

		public SubmitApplicationState Handle(ApplicationStartedV1 message)
		{
			return new SubmitApplicationState(true, ApplicationHasBeenSubmitted);
		}
	}
}
