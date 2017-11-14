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
	public class SubscriptionStreamProvisioningScriptController :
	    IMessageHandler<IRecordedEvent<IStartProvisionSubscriptionStreamScript>, Task>,
		IMessageHandler<IRecordedEvent<ISubscriberStopped>, Task>,
		IMessageHandler<IRecordedEvent<ISubscriberStarted>, Task>,
		IMessageHandler<IRecordedEvent<ISubscriptionStreamProvisioned>, Task>
	{
		private readonly IEventPublisher _eventPublisher;
		private readonly IScriptDefinition<SubscriptionStreamProvisioningScriptData> _scriptDefinition;
		
		public SubscriptionStreamProvisioningScriptController
			(
				IEventPublisher eventPublisher,
				IScriptDefinition<SubscriptionStreamProvisioningScriptData> scriptDefinition
			)
		{
			_scriptDefinition = scriptDefinition;
			_eventPublisher = eventPublisher;
		}

		public Task Handle(IRecordedEvent<ISubscriberStopped> message)
		{
			return ScriptCommand.ProcessNextScriptActivity(_eventPublisher, _scriptDefinition, message);
		}

		public Task Handle(IRecordedEvent<ISubscriberStarted> message)
		{
			return ScriptCommand.ProcessNextScriptActivity(_eventPublisher, _scriptDefinition, message);
		}

		public Task Handle(IRecordedEvent<ISubscriptionStreamProvisioned> message)
		{
			return ScriptCommand.ProcessNextScriptActivity(_eventPublisher, _scriptDefinition, message);
		}

		public Task Handle(IRecordedEvent<IStartProvisionSubscriptionStreamScript> message)
		{
			return ScriptCommand.StartScript
				(
					_eventPublisher,
					_scriptDefinition,
					new SubscriptionStreamProvisioningScriptData
					{
						SubscriberName = message.Data.SubscriberName,
						SubscriptionStreamName = message.Data.SubscriptionStreamName
					}
				);
		}

		
	}
}
