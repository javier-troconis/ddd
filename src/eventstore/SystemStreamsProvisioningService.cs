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
        private readonly ITopicStreamProvisioner _topicStreamProvisioner;
        private readonly ISubscriptionStreamProvisioningService _subscriptionStreamProvisioningService;

        public SystemStreamsProvisioningService(
            ITopicStreamProvisioner topicStreamProvisioner, 
            ISubscriptionStreamProvisioningService subscriptionStreamProvisioningService
            )
		{
            _topicStreamProvisioner = topicStreamProvisioner;
            _subscriptionStreamProvisioningService = subscriptionStreamProvisioningService;
        }

		public Task ProvisionSystemStreams()
		{
            return Task.WhenAll
                (
                    _topicStreamProvisioner.ProvisionTopicStream(),
                    _subscriptionStreamProvisioningService
                        .RegisterSubscriptionStream<IProvisionPersistentSubscriptionRequests>()
                        .RegisterSubscriptionStream<IProvisionSubscriptionStreamRequests>()
                        .ProvisionAllSubscriptionStreams()
                );
		}

		
	}
}
