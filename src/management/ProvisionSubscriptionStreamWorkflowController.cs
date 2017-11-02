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
	public class ProvisionSubscriptionStreamWorkflowController :
	    IMessageHandler<IRecordedEvent<IStartProvisionSubscriptionStreamWorkflow>, Task>,
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
			if (!message.Metadata.TryGetValue(EventHeaderKey.WorkflowType, out object workflowType) || !Equals(workflowType, WorkflowType))
			{
				return Task.CompletedTask;
			}
				
			Console.WriteLine($"{nameof(ProvisionSubscriptionStreamWorkflowController)} {message.Metadata[EventHeaderKey.WorkflowId]} handling: {nameof(ISubscriberStopped)}");
			var workflowData = JsonConvert.DeserializeObject<WorkflowData>((string)message.Metadata[EventHeaderKey.WorkflowData]);
			return _eventPublisher.PublishEvent
				(
					new ProvisionSubscriptionStream(workflowData.SubscriptionStreamName), 
					x => x.CopyMetadata(message.Metadata)
				);
		}

		public Task Handle(IRecordedEvent<ISubscriberStarted> message)
		{
			if (message.Metadata.TryGetValue(EventHeaderKey.WorkflowType, out object workflowType) && Equals(workflowType, WorkflowType))
			{
				Console.WriteLine($"{nameof(ProvisionSubscriptionStreamWorkflowController)} {message.Metadata[EventHeaderKey.WorkflowId]} handling: {nameof(ISubscriberStarted)}");
			}
			return Task.CompletedTask;
		}

		public Task Handle(IRecordedEvent<IStartProvisionSubscriptionStreamWorkflow> message)
		{
			Console.WriteLine($"{nameof(ProvisionSubscriptionStreamWorkflowController)} {message.Data.WorkflowId} handling: {nameof(IStartProvisionSubscriptionStreamWorkflow)}");
			return _eventPublisher.PublishEvent(
				new StopSubscriber(message.Data.SubscriberName), 
				x => x
					.SetMetadata(EventHeaderKey.WorkflowId, message.Data.WorkflowId)
					.SetMetadata(EventHeaderKey.WorkflowType, WorkflowType)
					.SetMetadata
					(
						EventHeaderKey.WorkflowData, 
						JsonConvert.SerializeObject
						(
							new WorkflowData
							{
								SubscriptionStreamName = message.Data.SubscriptionStreamName,
								SubscriberName = message.Data.SubscriberName
							}
						)
					)
				);
		}

		public Task Handle(IRecordedEvent<ISubscriptionStreamProvisioned> message)
		{
			if (!message.Metadata.TryGetValue(EventHeaderKey.WorkflowType, out object workflowType) || !Equals(workflowType, WorkflowType))
			{
				return Task.CompletedTask;
			}

			Console.WriteLine($"{nameof(ProvisionSubscriptionStreamWorkflowController)} {message.Metadata[EventHeaderKey.WorkflowId]} handling: {nameof(ISubscriptionStreamProvisioned)}");
			var workflowData = JsonConvert.DeserializeObject<WorkflowData>((string)message.Metadata[EventHeaderKey.WorkflowData]);
			return _eventPublisher.PublishEvent
				(
					new StartSubscriber(workflowData.SubscriberName),
					x => x.CopyMetadata(message.Metadata)
				);
		}

		private class WorkflowData
		{
			public string SubscriptionStreamName { get; set; }
			public string SubscriberName { get; set; }
		}
	}
}
