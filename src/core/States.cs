using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using shared;

namespace core
{
	public struct SubmitApplicationState :
		 IMessageHandler<ApplicationStartedV1, SubmitApplicationState>,
		 IMessageHandler<ApplicationSubmittedV1, SubmitApplicationState>
	{
		public readonly bool HasBeenStarted;
		public readonly bool HasBeenSubmitted;

		public SubmitApplicationState(bool hasBeenStarted, bool hasBeenSubmitted)
		{
			HasBeenStarted = hasBeenStarted;
			HasBeenSubmitted = hasBeenSubmitted;
		}

		public SubmitApplicationState Handle(ApplicationSubmittedV1 message)
		{
			return new SubmitApplicationState(HasBeenStarted, true);
		}

		public SubmitApplicationState Handle(ApplicationStartedV1 message)
		{
			return new SubmitApplicationState(true, HasBeenSubmitted);
		}
	}
}
