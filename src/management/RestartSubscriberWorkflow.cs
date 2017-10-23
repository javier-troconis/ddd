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
	public class RestartSubscriberWorkflow :
	    IMessageHandler<IRecordedEvent<IStartRestartSubscriberWorkflow>, Task>,
		IMessageHandler<IRecordedEvent<ISubscriberStopped>, Task>,
		IMessageHandler<IRecordedEvent<ISubscriberStarted>, Task>
	{
		private readonly IEventStore _eventStore;

		public RestartSubscriberWorkflow(IEventStore eventStore)
		{
			_eventStore = eventStore;
		}

		public async Task Handle(IRecordedEvent<ISubscriberStopped> message)
		{
			if (!message.Metadata.TryGetValue(EventHeaderKey.WorkflowId, out object workflowId))
			{
				Console.WriteLine("from: " + nameof(RestartSubscriberWorkflow) + " ignoring: " + nameof(ISubscriberStopped));
				return;
			}
			Console.WriteLine("from: " + nameof(RestartSubscriberWorkflow) + " processing: " + nameof(ISubscriberStopped));
			IEventPublisher eventPublisher = new EventPublisher(_eventStore);
			await eventPublisher.PublishEvent(new StartSubscriber(message.Data.SubscriberName), x => x.SetMetadata(EventHeaderKey.WorkflowId, workflowId));
		}

		public async Task Handle(IRecordedEvent<IStartRestartSubscriberWorkflow> message)
		{
			Console.WriteLine("from: " + nameof(RestartSubscriberWorkflow) + " processing: " + nameof(IStartRestartSubscriberWorkflow));
			IEventPublisher eventPublisher = new EventPublisher(_eventStore);
			await eventPublisher.PublishEvent(new StopSubscriber(message.Data.SubscriberName), x => x.SetMetadata(EventHeaderKey.WorkflowId, message.Data.WorkflowId));
		}

		public Task Handle(IRecordedEvent<ISubscriberStarted> message)
		{
			if (!message.Metadata.TryGetValue(EventHeaderKey.WorkflowId, out object workflowId))
			{
				Console.WriteLine("from: " + nameof(RestartSubscriberWorkflow) + " ignoring: " + nameof(ISubscriberStarted));
				return Task.CompletedTask;
			}
			Console.WriteLine("from: " + nameof(RestartSubscriberWorkflow) + " processing: " + nameof(ISubscriberStarted));
			return Task.CompletedTask;
		}
	}
}
