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
	public class RestartSubscriberWorkflow1 :
		IMessageHandler<IRecordedEvent<IStartRestartSubscriberWorkflow1>, Task>,
		IMessageHandler<IRecordedEvent<ISubscriberStopped>, Task>,
		IMessageHandler<IRecordedEvent<ISubscriberStarted>, Task>
	{
		private static readonly string WorkflowType = typeof(RestartSubscriberWorkflow1).FullName;
		private readonly IEventPublisher _eventPublisher;

		public RestartSubscriberWorkflow1(IEventPublisher eventPublisher)
		{
			_eventPublisher = eventPublisher;
		}

		public Task Handle(IRecordedEvent<ISubscriberStopped> message)
		{
			if (!message.Metadata.TryGetValue(EventHeaderKey.WorkflowType, out object workflowType) ||
			    !Equals(workflowType, WorkflowType)) return Task.CompletedTask;
			Console.WriteLine($"{nameof(RestartSubscriberWorkflow1)} {message.Metadata[EventHeaderKey.WorkflowId]} handling: {nameof(ISubscriberStopped)}");
			return _eventPublisher.PublishEvent(
				new StartSubscriber(message.Data.SubscriberName), 
				x => x
					.SetMetadata(EventHeaderKey.WorkflowId, message.Metadata[EventHeaderKey.WorkflowId])
					.SetMetadata(EventHeaderKey.WorkflowType, WorkflowType));
		}

		public Task Handle(IRecordedEvent<ISubscriberStarted> message)
		{
			if (message.Metadata.TryGetValue(EventHeaderKey.WorkflowType, out object workflowType) && Equals(workflowType, WorkflowType))
			{
				Console.WriteLine($"{nameof(RestartSubscriberWorkflow1)} {message.Metadata[EventHeaderKey.WorkflowId]} handling: {nameof(ISubscriberStarted)}");
			}
			return Task.CompletedTask;
			
		}

		public Task Handle(IRecordedEvent<IStartRestartSubscriberWorkflow1> message)
		{
			Console.WriteLine($"{nameof(RestartSubscriberWorkflow1)} {message.Data.WorkflowId} handling: {nameof(IStartRestartSubscriberWorkflow1)}");
			return _eventPublisher.PublishEvent(
				new StopSubscriber(message.Data.SubscriberName), 
				x => x
					.SetMetadata(EventHeaderKey.WorkflowId, message.Data.WorkflowId)
					.SetMetadata(EventHeaderKey.WorkflowType, WorkflowType));
		}
	}
}
