using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace eventstore
{
    internal static class EventHeaderKey
    {
	    public const string Topics = "topics";
		public const string CorrelationId = "correlationId";
	}
}
