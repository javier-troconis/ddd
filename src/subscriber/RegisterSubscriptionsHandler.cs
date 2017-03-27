using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using infra;

using shared;

using subscriber.contracts;

namespace subscriber
{
	public class RegisterSubscriptionsHandler : IRegisterSubscriptionsHandler
	{
		public Task Handle(IRecordedEvent<ISubscriptionsRegistrationRequested> message)
		{
			Console.WriteLine(typeof(ISubscriptionsRegistrationRequested).FullName);
			return Task.CompletedTask;
		}
	}
}
