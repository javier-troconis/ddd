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

		public class WhenSubmittingState : ValueType<WhenSubmittingState>, 
			IEventConsumer<ApplicationStarted, WhenSubmittingState>, 
			IEventConsumer<ApplicationSubmitted, WhenSubmittingState>
		{
			public bool HasBeenStarted { get; }
			public bool HasBeenSubmitted { get; }

			public WhenSubmittingState()
			{

			}

			private WhenSubmittingState(bool hasBeenStarted, bool hasBeenSubmitted)
			{
				HasBeenStarted = hasBeenStarted;
				HasBeenSubmitted = hasBeenSubmitted;
			}

			public WhenSubmittingState When(ApplicationSubmitted @event)
			{
				return new WhenSubmittingState(HasBeenStarted, true);
			}

			public WhenSubmittingState When(ApplicationStarted @event)
			{
				return new WhenSubmittingState(true, HasBeenSubmitted);
			}
		}

		public static IEnumerable<IEvent> Submit(WhenSubmittingState state)
		{
			Ensure.NotNull(state, nameof(WhenSubmittingState));
			if (!state.HasBeenStarted)
			{
				throw new Exception("application has not been started");
			}
			if (state.HasBeenSubmitted)
			{
				throw new Exception("application has already been submitted");
			}
			yield return new ApplicationSubmitted();
		}
	}
}
