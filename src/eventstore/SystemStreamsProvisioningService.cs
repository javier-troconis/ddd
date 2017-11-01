using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Net;

using EventStore.ClientAPI.Exceptions;

using shared;

namespace eventstore
{
	public interface ISystemStreamsProvisioningService
	{
		Task ProvisionSystemStreams();
	}

	public class SystemStreamsProvisioningService : ISystemStreamsProvisioningService
	{
        private readonly ITopicStreamProvisioningService _topicStreamProvisioningService;
        private readonly ISubscriptionStreamProvisioningService _subscriptionStreamProvisioningService;

        public SystemStreamsProvisioningService(
            ITopicStreamProvisioningService topicStreamProvisioningService, 
            ISubscriptionStreamProvisioningService subscriptionStreamProvisioningService
            )
		{
            _topicStreamProvisioningService = topicStreamProvisioningService;
            _subscriptionStreamProvisioningService = subscriptionStreamProvisioningService;
        }

		public Task ProvisionSystemStreams()
		{
            return Task.WhenAll
                (
                    _topicStreamProvisioningService.ProvisionTopicStream(),
                    _subscriptionStreamProvisioningService
                        .RegisterSubscriptionStream<IPersistentSubscriptionProvisioningController>()
                        .RegisterSubscriptionStream<ISubscriptionStreamProvisioningController>()
                        .ProvisionAllSubscriptionStreams()
                );
		}

		
	}
}
