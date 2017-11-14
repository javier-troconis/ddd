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
	public class ProvisionSubscriptionStreamScriptController :
	    IMessageHandler<IRecordedEvent<IStartProvisionSubscriptionStreamScript>, Task>,
		IMessageHandler<IRecordedEvent<ISubscriberStopped>, Task>,
		IMessageHandler<IRecordedEvent<ISubscriberStarted>, Task>,
		IMessageHandler<IRecordedEvent<ISubscriptionStreamProvisioned>, Task>
	{
		private readonly IScriptDefinition<ProvisionSubscriptionStreamScriptData> _scriptDefinition;
		private readonly IEventPublisher _eventPublisher;

		public ProvisionSubscriptionStreamScriptController
			(
				IScriptDefinition<ProvisionSubscriptionStreamScriptData> scriptDefinition, 
				IEventPublisher eventPublisher
			)
		{
			_scriptDefinition = scriptDefinition;
			_eventPublisher = eventPublisher;
		}

		public Task Handle(IRecordedEvent<ISubscriberStopped> message)
		{
			return ScriptCommand.ProcessNextScriptActivity(_eventPublisher, _scriptDefinition.Activities, _scriptDefinition.Type, message);
		}

		public Task Handle(IRecordedEvent<ISubscriberStarted> message)
		{
			return ScriptCommand.ProcessNextScriptActivity(_eventPublisher, _scriptDefinition.Activities, _scriptDefinition.Type, message);
		}

		public Task Handle(IRecordedEvent<ISubscriptionStreamProvisioned> message)
		{
			return ScriptCommand.ProcessNextScriptActivity(_eventPublisher, _scriptDefinition.Activities, _scriptDefinition.Type, message);
		}

		public Task Handle(IRecordedEvent<IStartProvisionSubscriptionStreamScript> message)
		{
			return ScriptCommand.StartScript
				(
					_eventPublisher,
					_scriptDefinition.Activities,
					_scriptDefinition.Type,
					new ProvisionSubscriptionStreamScriptData
					{
						SubscriberName = message.Data.SubscriberName,
						SubscriptionStreamName = message.Data.SubscriptionStreamName
					}
				);
		}

		
	}
}
