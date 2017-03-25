﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using contracts;

namespace core
{
	public struct ApplicationStartedV1 : IApplicationStartedV1
	{

	}

	public struct ApplicationStartedV2 : IApplicationStartedV2
	{

	}

	public struct ApplicationStartedV3 : IApplicationStartedV3
	{

	}

	public struct ApplicationSubmittedV1 : IApplicationSubmittedV1
	{
		public ApplicationSubmittedV1(string submittedBy)
		{
			SubmittedBy = submittedBy;
		}

		public string SubmittedBy { get; }
	}
}