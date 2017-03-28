using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using eventstore;

using management.contracts;

using shared;


namespace subscriber
{
	public class SubscriptionRegistrationRequestedHandler : ISubscriptionRegistrationRequestedHandler
	{
		private readonly string _serviceName;

		public SubscriptionRegistrationRequestedHandler(string serviceName)
		{
			_serviceName = serviceName;
		}

		public Task Handle(IRecordedEvent<ISubscriptionRegistrationRequested> message)
		{
			if (string.Equals(_serviceName, message.Event.ServiceName))
			{
				Console.WriteLine("registering subscriptions");
			}
			return Task.CompletedTask;
		}

	}
}
