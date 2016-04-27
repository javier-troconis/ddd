using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using shared;

namespace core
{
	public struct WhenSubmittingApplicationState : IEventHandler<ApplicationStarted, WhenSubmittingApplicationState>,
		IEventHandler<ApplicationSubmitted, WhenSubmittingApplicationState>
	{
		public readonly bool HasBeenStarted;
		public readonly bool HasBeenSubmitted;

		public WhenSubmittingApplicationState(bool hasBeenStarted, bool hasBeenSubmitted)
		{
			HasBeenStarted = hasBeenStarted;
			HasBeenSubmitted = hasBeenSubmitted;
		}

		public WhenSubmittingApplicationState Handle(ApplicationSubmitted @event)
		{
			return new WhenSubmittingApplicationState(HasBeenStarted, true);
		}

		public WhenSubmittingApplicationState Handle(ApplicationStarted @event)
		{
			return new WhenSubmittingApplicationState(true, HasBeenSubmitted);
		}
	}

	
}
