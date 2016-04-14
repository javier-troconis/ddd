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
		public static IEnumerable<Event> Submit(ApplicationSubmitState state)
		{
			if (state.HasBeenSubmitted)
			{
				throw new Exception("application has already been submitted");
			}
			yield return new ApplicationSubmitted();
		}
	}
}
