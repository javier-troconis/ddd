using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using shared;

namespace core
{
	public struct WhenSubmittingApplicationState : IEventConsumer<ApplicationStarted, WhenSubmittingApplicationState>,
		IEventConsumer<ApplicationSubmitted, WhenSubmittingApplicationState>
	{
		public readonly bool HasBeenStarted;
		public readonly bool HasBeenSubmitted;

		public WhenSubmittingApplicationState(bool hasBeenStarted, bool hasBeenSubmitted)
		{
			HasBeenStarted = hasBeenStarted;
			HasBeenSubmitted = hasBeenSubmitted;
		}

		public WhenSubmittingApplicationState Apply(ApplicationSubmitted @event)
		{
			return new WhenSubmittingApplicationState(HasBeenStarted, true);
		}

		public WhenSubmittingApplicationState Apply(ApplicationStarted @event)
		{
			return new WhenSubmittingApplicationState(true, HasBeenSubmitted);
		}
	}
}
