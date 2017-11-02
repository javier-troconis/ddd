using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace eventstore
{
	internal static class EventMetadataKey
	{
		public const string Topics = "__topics";
		public const string CorrelationId = "__correlationId";
	}
}
