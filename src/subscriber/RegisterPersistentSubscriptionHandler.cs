using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using eventstore;

using EventStore.ClientAPI.Common.Log;

using management.contracts;

using shared;


namespace subscriber
{
	public class RegisterPersistentSubscriptionHandler : IRegisterPersistentSubscriptionHandler
	{
		private readonly string _serviceName;

		public RegisterPersistentSubscriptionHandler(string serviceName)
		{
			_serviceName = serviceName;
		}

		public Task Handle(IRecordedEvent<IRegisterPersistentSubscription> message)
		{
			
		}

	}
}
