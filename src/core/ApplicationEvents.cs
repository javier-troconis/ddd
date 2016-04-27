using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using shared;

namespace core
{
	public struct ApplicationStarted : IEvent
	{

	}

	public struct ApplicationSubmitted : IEvent
	{
		public readonly string SubmittedBy;

		public ApplicationSubmitted(string submittedBy)
		{
			SubmittedBy = submittedBy;
		}
	}
}
