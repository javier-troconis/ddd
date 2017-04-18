using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using eventstore;

using shared;

namespace query
{
	public class SubscriptionStreamsProvisioningRequestsHandler : ISubscriptionStreamsProvisioningRequests
	{
		private readonly ISubscriptionStreamProvisioner _subscriptionStreamProvisioner;

		public SubscriptionStreamsProvisioningRequestsHandler(ISubscriptionStreamProvisioner subscriptionStreamProvisioner)
		{
			_subscriptionStreamProvisioner = subscriptionStreamProvisioner;
		}

		public Task Handle(IRecordedEvent<ISubscriptionStreamsProvisioningRequested> message)
		{
			Console.WriteLine("calling " + nameof(SubscriptionStreamsProvisioningRequestsHandler) + " " + message.EventId);
			return _subscriptionStreamProvisioner
				.IncludeSubscriptionStream<Subscriber1>()
				.IncludeSubscriptionStream<Subscriber2>()
				.IncludeSubscriptionStream<Subscriber3>()
				.ProvisionSubscriptionStreams(message.Event.SubscriptionStream);
		}
	}
}

