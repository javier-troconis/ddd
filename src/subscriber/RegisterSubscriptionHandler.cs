using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using management.contracts;

using shared;


namespace subscriber
{
	public class RegisterSubscriptionHandler : IMessageHandler<IRecordedEvent<ISubscriptionRegistrationRequested>, Task>
	{
		public Task Handle(IRecordedEvent<ISubscriptionRegistrationRequested> message)
		{
			Console.WriteLine(typeof(ISubscriptionRegistrationRequested).FullName);
			return Task.CompletedTask;
		}
	}
}
