using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace es
{
	public class ApplicationStarted : Event<ApplicationStarted>
	{

	}

	public class ApplicationSubmitted : Event<ApplicationSubmitted>
	{

	}

	public class Application : Aggregate
	{
		public Application(Guid id) : base(id)
		{

		}

		public void Submit()
		{
			RecordThat(new ApplicationSubmitted());
		}

		public void Start()
		{
			RecordThat(new ApplicationStarted());
		}
	}
}
