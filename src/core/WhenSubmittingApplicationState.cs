using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using shared;

namespace core
{
	public class WhenSubmittingApplicationState : 
		IEventConsumer<ApplicationStarted, WhenSubmittingApplicationState>,
		IEventConsumer<ApplicationSubmitted, WhenSubmittingApplicationState>
	{
		public bool HasBeenStarted { get; }
		public bool HasBeenSubmitted { get; }

		public WhenSubmittingApplicationState()
		{

		}

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
