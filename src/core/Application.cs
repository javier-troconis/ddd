using System;
using shared;

namespace core
{
	public class ApplicationStarted : Event<ApplicationStarted>
	{
		
	}

	public class ApplicationSubmitted : Event<ApplicationSubmitted>
	{

	}

	public class Application : AggregateRoot
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
