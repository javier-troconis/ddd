using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using shared;

namespace core
{
	public class ApplicationStarted : ValueType<ApplicationStarted>, IEvent
	{

	}

	public class ApplicationSubmitted : ValueType<ApplicationSubmitted>, IEvent
	{

	}
}
