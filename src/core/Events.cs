using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using contracts;

namespace core
{
	public class ApplicationStartedV1 : IApplicationStartedV1
	{

	}

	public class ApplicationStartedV2 : IApplicationStartedV2
	{

	}

	public class ApplicationStartedV3 : IApplicationStartedV3
	{

	}

	public class ApplicationSubmittedV1 : IApplicationSubmittedV1
	{
		public ApplicationSubmittedV1(string submittedBy)
		{
			SubmittedBy = submittedBy;
		}

		public string SubmittedBy { get; }
	}
}
