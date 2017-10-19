using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using eventstore;
using management.contracts;
using shared;

namespace management
{
    public class RestartSubscriberWorkflow : 
		IMessageHandler<IRecordedEvent<ISubscriptionStopped>, Task>,
	    IMessageHandler<IRecordedEvent<ISubscriptionStarted>, Task>
	{
		private readonly IEventStore _eventStore;

		public RestartSubscriberWorkflow(IEventStore eventStore)
		{
			_eventStore = eventStore;
		}

		public Task Handle(IRecordedEvent<ISubscriptionStopped> message)
		{
			if (message.Metadata.TryGetValue(EventHeaderKey.WorkflowName, out object workflowName))
			{
				
			}
			return Task.CompletedTask;
		}

		public Task Handle(IRecordedEvent<ISubscriptionStarted> message)
		{
			throw new NotImplementedException();
		}
	}
}
