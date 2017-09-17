using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Net;

using EventStore.ClientAPI.Exceptions;

using shared;

namespace eventstore
{
	public interface ISystemStreamsProvisioner
	{
		Task ProvisionSystemStreams();
	}

	public class SystemStreamsProvisioner : ISystemStreamsProvisioner
	{
        private readonly ITopicStreamProvisioner _topicStreamProvisioner;
        private readonly ISubscriptionStreamProvisioner _subscriptionStreamProvisioner;

        public SystemStreamsProvisioner(
            ITopicStreamProvisioner topicStreamProvisioner, 
            ISubscriptionStreamProvisioner subscriptionStreamProvisioner
            )
		{
            _topicStreamProvisioner = topicStreamProvisioner;
            _subscriptionStreamProvisioner = subscriptionStreamProvisioner;
        }

		public Task ProvisionSystemStreams()
		{
            return Task.WhenAll
                (
                    _topicStreamProvisioner.ProvisionTopicStream(),
                    _subscriptionStreamProvisioner
                        .RegisterSubscriptionStreamProvisioning<IPersistentSubscriptionsProvisioningRequests>()
                        .RegisterSubscriptionStreamProvisioning<ISubscriptionStreamsProvisioningRequests>()
                        .ProvisionSubscriptionStream()
                );
		}

		
	}
}
