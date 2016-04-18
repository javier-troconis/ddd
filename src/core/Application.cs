using System;
using System.Collections.Generic;
using shared;

namespace core
{
	public class ApplicationStarted : ValueType<ApplicationStarted>, IEvent
	{
		
	}

	public class ApplicationSubmitted : ValueType<ApplicationSubmitted>, IEvent
	{

	}

	public static class Application
	{
		public static IEnumerable<IEvent> Start()
		{
			yield return new ApplicationStarted();
		}

		public class WhenSubmittingState : ValueType<WhenSubmittingState>, IEventConsumer<ApplicationSubmitted, WhenSubmittingState>
		{
			public bool HasBeenSubmitted { get; }

			public WhenSubmittingState()
			{

			}

			private WhenSubmittingState(bool hasBeenSubmitted)
			{
				HasBeenSubmitted = hasBeenSubmitted;
			}

			public WhenSubmittingState When(ApplicationSubmitted @event)
			{
				return new WhenSubmittingState(true);
			}
		}

		public static IEnumerable<IEvent> Submit(WhenSubmittingState state)
		{
			Ensure.NotNull(state, "WhenSubmittingState");
			if (state.HasBeenSubmitted)
			{
				throw new Exception("application has already been submitted");
			}
			yield return new ApplicationSubmitted();
		}
	}
}
