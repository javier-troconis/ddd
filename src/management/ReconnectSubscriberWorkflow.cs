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
	public static class ReconnectSubscriberWorkflowStep
	{
		public const string StopSubscriberInitiated = nameof(StopSubscriberInitiated);
		public const string StopSubscriberCompleted = nameof(StopSubscriberCompleted);
		public const string StartSubscriberInitiated = nameof(StartSubscriberInitiated);
		public const string StartSubscriberCompleted = nameof(StartSubscriberCompleted);
	}


	public class ReconnectSubscriberWorkflow :
	    IMessageHandler<IRecordedEvent<IStartReconnectSubscriberWorkflow>, Task>,
		IMessageHandler<IRecordedEvent<ISubscriptionStopped>, Task>,
	    IMessageHandler<IRecordedEvent<ISubscriptionStarted>, Task>
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
			var streamName = "restartsubscriberworkflow-" + Guid.Parse((string)workflowId).ToString("N");
			await _eventStore.WriteEvents(streamName, 0, new object[] { new WorkflowStepExecuted(ReconnectSubscriberWorkflowStep.StopSubscriberCompleted) });
			IEventPublisher eventPublisher = new EventPublisher(_eventStore);
			await eventPublisher.PublishEvent(new StartSubscription("query_subscriber3"), x => x.SetEventHeader(EventHeaderKey.WorkflowId, workflowId));
			await _eventStore.WriteEvents(streamName, 1, new object[] { new WorkflowStepExecuted(ReconnectSubscriberWorkflowStep.StartSubscriberInitiated) });
		}

		public async Task Handle(IRecordedEvent<ISubscriptionStarted> message)
		{
			if (!message.Metadata.TryGetValue(EventHeaderKey.WorkflowId, out object workflowId))
			{
				return;
			}
			var streamName = "restartsubscriberworkflow-" + Guid.Parse((string)workflowId).ToString("N");
			await _eventStore.WriteEvents(streamName, 2, new object[] { new WorkflowStepExecuted(ReconnectSubscriberWorkflowStep.StartSubscriberCompleted) });
		}

		public async Task Handle(IRecordedEvent<IStartReconnectSubscriberWorkflow> message)
		{
			var workflowId = Guid.NewGuid();
			var streamName = "restartsubscriberworkflow-" + workflowId.ToString("N");
			await _eventStore.WriteEvents(streamName, ExpectedVersion.NoStream, new object[] { new WorkflowStepExecuted(ReconnectSubscriberWorkflowStep.StopSubscriberInitiated)  });
			IEventPublisher eventPublisher = new EventPublisher(_eventStore);
			await eventPublisher.PublishEvent(new StopSubscription("query_subscriber3"), x => x.SetEventHeader(EventHeaderKey.WorkflowId, workflowId));
		}
	}
}
