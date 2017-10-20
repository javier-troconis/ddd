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
		IMessageHandler<IRecordedEvent<ISubscriberStopped>, Task>,
		IMessageHandler<IRecordedEvent<ISubscriberStarted>, Task>
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
				Console.WriteLine("from: " + nameof(ReconnectSubscriberWorkflow) + " ignoring: " + nameof(ISubscriberStopped));
				return;
			}
			Console.WriteLine("from: " + nameof(ReconnectSubscriberWorkflow) + " processing: " + nameof(ISubscriberStopped));
			IEventPublisher eventPublisher = new EventPublisher(_eventStore);
			await eventPublisher.PublishEvent(new StartSubscriber(message.Body.SubscriberName), x => x.SetEntry(EventHeaderKey.WorkflowId, workflowId));
		}

		public async Task Handle(IRecordedEvent<IStartReconnectSubscriberWorkflow> message)
		{
			Console.WriteLine("from: " + nameof(ReconnectSubscriberWorkflow) + " processing: " + nameof(IStartReconnectSubscriberWorkflow));
			IEventPublisher eventPublisher = new EventPublisher(_eventStore);
			await eventPublisher.PublishEvent(new StopSubscriber(message.Body.SubscriberName), x => x.SetEntry(EventHeaderKey.WorkflowId, message.Body.WorkflowId));
		}

		public Task Handle(IRecordedEvent<ISubscriberStarted> message)
		{
			if (!message.Header.TryGetValue(EventHeaderKey.WorkflowId, out object workflowId))
			{
				Console.WriteLine("from: " + nameof(ReconnectSubscriberWorkflow) + " ignoring: " + nameof(ISubscriberStarted));
				return Task.CompletedTask;
			}
			Console.WriteLine("from: " + nameof(ReconnectSubscriberWorkflow) + " processing: " + nameof(ISubscriberStarted));
			return Task.CompletedTask;
		}
	}
}
