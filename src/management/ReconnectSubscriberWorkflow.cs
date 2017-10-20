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
	    IMessageHandler<IRecordedEvent<IRunReconnectSubscriberWorkflow>, Task>,
		IMessageHandler<IRecordedEvent<ISubscriberStopped>, Task>
	{
		private readonly IEventStore _eventStore;

		public ReconnectSubscriberWorkflow(IEventStore eventStore)
		{
			_eventStore = eventStore;
		}

		public async Task Handle(IRecordedEvent<ISubscriberStopped> message)
		{
			if (!message.Header.TryGetValue(EventHeaderKey.WorkflowId, out object workflowId))
			{
				return;
			}
			IEventPublisher eventPublisher = new EventPublisher(_eventStore);
			await eventPublisher.PublishEvent(new StartSubscriber(message.Body.SubscriberName), x => x.SetEventHeader(EventHeaderKey.WorkflowId, workflowId));
		}

		public async Task Handle(IRecordedEvent<IRunReconnectSubscriberWorkflow> message)
		{
			IEventPublisher eventPublisher = new EventPublisher(_eventStore);
			await eventPublisher.PublishEvent(new StopSubscriber(message.Body.SubscriberName), x => x.SetEventHeader(EventHeaderKey.WorkflowId, message.Body.WorkflowId));
		}
	}
}
