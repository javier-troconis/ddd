using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using shared;

namespace core
{
	public struct WhenSubmittingApplicationState : 
        IMessageHandler<ApplicationStarted, WhenSubmittingApplicationState>,
		IMessageHandler<ApplicationSubmitted, WhenSubmittingApplicationState>
	{
		public readonly bool HasBeenStarted;
		public readonly bool HasBeenSubmitted;

		public WhenSubmittingApplicationState(bool hasBeenStarted, bool hasBeenSubmitted)
		{
			HasBeenStarted = hasBeenStarted;
			HasBeenSubmitted = hasBeenSubmitted;
		}

		public WhenSubmittingApplicationState Handle(ApplicationSubmitted message)
		{
			return new WhenSubmittingApplicationState(HasBeenStarted, true);
		}

		public WhenSubmittingApplicationState Handle(ApplicationStarted message)
		{
			return new WhenSubmittingApplicationState(true, HasBeenSubmitted);
		}
	}
}
