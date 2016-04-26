using System;
using System.Collections.Generic;
using shared;

namespace core
{
	public static class ApplicationAction
	{
		public static IEnumerable<IEvent> Start()
		{
			return DoStart();
		}

		private static IEnumerable<IEvent> DoStart()
		{
			yield return new ApplicationStarted();
		}

		public static IEnumerable<IEvent> Submit(WhenSubmittingApplicationState state, string submitter)
		{
			Ensure.NotNull(state, nameof(state));
			if (!state.HasBeenStarted)
			{
				throw new Exception("application has not been started");
			}
			if (state.HasBeenSubmitted)
			{
				throw new Exception("application has already been submitted");
			}
			return DoSubmit(state, submitter);
		}

		private static IEnumerable<IEvent> DoSubmit(WhenSubmittingApplicationState state, string submitter)
		{
			yield return new ApplicationSubmitted(submitter);
		}
	}
}
