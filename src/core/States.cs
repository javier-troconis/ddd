using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using contracts;

using shared;

namespace core
{
	public struct SubmitApplicationState :
		 IMessageHandler<IApplicationStartedV1, SubmitApplicationState>,
		 IMessageHandler<IApplicationSubmittedV1, SubmitApplicationState>
	{
		public readonly bool HasBeenStarted;
		public readonly bool HasBeenSubmitted;

		public SubmitApplicationState(bool hasBeenStarted, bool hasBeenSubmitted)
		{
			HasBeenStarted = hasBeenStarted;
			HasBeenSubmitted = hasBeenSubmitted;
		}

		public SubmitApplicationState Handle(IApplicationSubmittedV1 message)
		{
			return new SubmitApplicationState(HasBeenStarted, true);
		}

		public SubmitApplicationState Handle(IApplicationStartedV1 message)
		{
			return new SubmitApplicationState(true, HasBeenSubmitted);
		}
	}
}
