using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using eventstore;
using EventStore.ClientAPI;
using management.contracts;
using shared;

namespace management
{
	public class ReconnectSubscriberWorkflow :
	    IMessageHandler<IRecordedEvent<IStartReconnectSubscriberWorkflow>, Task>,
		IMessageHandler<IRecordedEvent<ISubscriptionStopped>, Task>
	{
		private readonly IEventStore _eventStore;

		public ReconnectSubscriberWorkflow(IEventStore eventStore)
		{
			_eventStore = eventStore;
		}

		public async Task Handle(IRecordedEvent<ISubscriptionStopped> message)
		{
			if (!message.Metadata.TryGetValue(EventHeaderKey.WorkflowId, out object workflowId))
			{
				return;
			}
			IEventPublisher eventPublisher = new EventPublisher(_eventStore);
			await eventPublisher.PublishEvent(new StartSubscription("query_subscriber3"), x => x.SetEventHeader(EventHeaderKey.WorkflowId, workflowId));
		}

		public async Task Handle(IRecordedEvent<IStartReconnectSubscriberWorkflow> message)
		{
			IEventPublisher eventPublisher = new EventPublisher(_eventStore);
			await eventPublisher.PublishEvent(new StopSubscription("query_subscriber3"), x => x.SetEventHeader(EventHeaderKey.WorkflowId, message.Body.WorkflowId));
		}
	}
}
