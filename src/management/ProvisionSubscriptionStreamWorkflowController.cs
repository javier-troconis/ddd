using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using eventstore;
using EventStore.ClientAPI;
using management.contracts;
using Newtonsoft.Json;
using shared;

namespace management
{
	//todo
	public class ProvisionSubscriptionStreamWorkflowController :
	    IMessageHandler<IRecordedEvent<IRunProvisionSubscriptionStreamWorkflow>, Task>,
		IMessageHandler<IRecordedEvent<ISubscriberStopped>, Task>,
		IMessageHandler<IRecordedEvent<ISubscriberStarted>, Task>,
		IMessageHandler<IRecordedEvent<ISubscriptionStreamProvisioned>, Task>
	{
		private static readonly string WorkflowType = typeof(ProvisionSubscriptionStreamWorkflowController).FullName;
		private readonly IEventPublisher _eventPublisher;

		public ProvisionSubscriptionStreamWorkflowController(IEventPublisher eventPublisher)
		{
			_eventPublisher = eventPublisher;
		}

		public Task Handle(IRecordedEvent<ISubscriberStopped> message)
		{
			WorkflowData workflowData;
			if (message.Metadata.TryGetValue(EventHeaderKey.WorkflowData, out object data) && (workflowData = JsonConvert.DeserializeObject<WorkflowData>((string)data)).WorkflowType == WorkflowType)
			{
				return RunNextActivity(workflowData, WorkflowActivities);
			}
			return Task.CompletedTask;
		}

		public Task Handle(IRecordedEvent<ISubscriberStarted> message)
		{
			WorkflowData workflowData;
			if (message.Metadata.TryGetValue(EventHeaderKey.WorkflowData, out object data) && (workflowData = JsonConvert.DeserializeObject<WorkflowData>((string)data)).WorkflowType == WorkflowType)
			{
				return RunNextActivity(workflowData, WorkflowActivities);
			}
			return Task.CompletedTask;
		}

		public Task Handle(IRecordedEvent<ISubscriptionStreamProvisioned> message)
		{
			WorkflowData workflowData;
			if (message.Metadata.TryGetValue(EventHeaderKey.WorkflowData, out object data) && (workflowData = JsonConvert.DeserializeObject<WorkflowData>((string)data)).WorkflowType == WorkflowType)
			{
				return RunNextActivity(workflowData, WorkflowActivities);
			}
			return Task.CompletedTask;
		}

		public Task Handle(IRecordedEvent<IRunProvisionSubscriptionStreamWorkflow> message)
		{
			var workflowData = new WorkflowData
			{
				WorkflowId = message.Data.WorkflowId,
				WorkflowType = WorkflowType,
				SubscriptionStreamName = message.Data.SubscriptionStreamName,
				SubscriberName = message.Data.SubscriberName
			};
			return RunNextActivity(workflowData, WorkflowActivities);
		}

		private Task RunNextActivity(WorkflowData workflowData, IReadOnlyList<Func<WorkflowData, object>> workflowActivities)
		{
			if (++workflowData.CurrentActivityIndex >= WorkflowActivities.Length)
			{
				return Task.CompletedTask;
			}
			var nextActivity = workflowActivities[workflowData.CurrentActivityIndex](workflowData);
			Console.WriteLine($"{nameof(ProvisionSubscriptionStreamWorkflowController)} {workflowData.WorkflowId} running: {nextActivity.GetType()}");
			return _eventPublisher.PublishEvent
			(
				nextActivity,
				x => x
					.SetMetadata(EventHeaderKey.WorkflowData, JsonConvert.SerializeObject(workflowData))
			);
		}

		private class WorkflowData
		{
			public Guid WorkflowId { get; set; }
			public string WorkflowType { get; set; }
			public int CurrentActivityIndex { get; set; } = -1;
			public string SubscriptionStreamName { get; set; }
			public string SubscriberName { get; set; }
		}

		private static readonly Func<WorkflowData, object>[] WorkflowActivities = 
			{
				x => new StopSubscriber(x.SubscriberName),
				x => new StartSubscriber(x.SubscriberName),
				x => new ProvisionSubscriptionStream(x.SubscriptionStreamName), 
				x => new StopSubscriber(x.SubscriberName),
				x => new ProvisionSubscriptionStream(x.SubscriptionStreamName),
				x => new StartSubscriber(x.SubscriberName),
				x => new StopSubscriber(x.SubscriberName),
				x => new StartSubscriber(x.SubscriberName)
			};
	}
}
