using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using shared;

namespace core
{
	public class ApplicationStarted : Value<ApplicationStarted>, IEvent
	{

	}

	public class ApplicationSubmitted : Value<ApplicationSubmitted>, IEvent
	{
		public readonly string SubmittedBy;

		public ApplicationSubmitted(string submittedBy)
		{
			SubmittedBy = submittedBy;
		}
	}
}
