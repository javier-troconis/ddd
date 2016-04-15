using System;
using System.Collections.Generic;
using shared;

namespace core
{
	public class ApplicationStarted : Event<ApplicationStarted>
	{
		
	}

	public class ApplicationSubmitted : Event<ApplicationSubmitted>
	{

	}

	public static class Application
	{
		public static IEnumerable<Event> Start()
		{
			yield return new ApplicationStarted();
		}

		public class SubmitState : IEventConsumer<ApplicationStarted>, IEventConsumer<ApplicationSubmitted>
		{
			public bool CanBeSubmitted { get; private set; }

			public void Apply(ApplicationSubmitted @event)
			{
				CanBeSubmitted = false;
			}

			public void Apply(ApplicationStarted @event)
			{
				CanBeSubmitted = true;
			}
		}

		public static IEnumerable<Event> Submit(SubmitState state)
		{
			Ensure.NotNull(state, "application submit state");
			if (!state.CanBeSubmitted)
			{
				throw new Exception("application cannot be submitted");
			}
			yield return new ApplicationSubmitted();
		}
	}
}
