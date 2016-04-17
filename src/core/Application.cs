using System;
using System.Collections.Generic;
using shared;

namespace core
{
	public class ApplicationStarted : IEvent
	{
		
	}

	public class ApplicationSubmitted : IEvent
	{

	}

	public static class Application
	{
		public static IEnumerable<IEvent> Start()
		{
			yield return new ApplicationStarted();
		}

		public class WhenSubmittingState : IEventConsumer<ApplicationStarted>, IEventConsumer<ApplicationSubmitted>
		{
			public bool HasBeenSubmitted { get; private set; }

			public void Apply(ApplicationSubmitted @event)
			{
				HasBeenSubmitted = true;
			}

			public void Apply(ApplicationStarted @event)
			{
				
			}
		}

		public static IEnumerable<IEvent> Submit(WhenSubmittingState state)
		{
			Ensure.NotNull(state, "WhenSubmittingState state");
			if (state.HasBeenSubmitted)
			{
				throw new Exception("application cannot be submitted");
			}
			yield return new ApplicationSubmitted();
		}
	}
}
