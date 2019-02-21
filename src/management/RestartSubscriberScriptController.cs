using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using eventstore;
using EventStore.ClientAPI;
using Microsoft.Extensions.Caching.Memory;
using shared;
using ISubscriberStarted = eventstore.ISubscriberStarted;
using ISubscriberStopped = eventstore.ISubscriberStopped;

namespace management
{
	public class RestartSubscriberScriptController :
		IMessageHandler<IRecordedEvent<ISubscriberStopped>, Task>,
		IMessageHandler<IRecordedEvent<ISubscriberStarted>, Task>
	{
		private readonly IScriptEvaluationService _scriptEvaluationService;
		private readonly IScriptDefinition<RestartSubscriberScriptData> _scriptDefinition;

		public RestartSubscriberScriptController(
			IScriptEvaluationService scriptEvaluationService,
			IScriptDefinition<RestartSubscriberScriptData> scriptDefinition)
		{
			_scriptEvaluationService = scriptEvaluationService;
			_scriptDefinition = scriptDefinition;
		}

		public Task Handle(IRecordedEvent<ISubscriberStopped> message)
		{
			return
				_scriptEvaluationService.StartNextScriptActivity(_scriptDefinition,
				ScriptFlowType.AscendingSequence,
				Convert.ToScriptExecutionContext<RestartSubscriberScriptData>(message.Metadata));
		}

		public Task Handle(IRecordedEvent<ISubscriberStarted> message)
		{
			return 
				_scriptEvaluationService.StartNextScriptActivity(_scriptDefinition, 
				ScriptFlowType.AscendingSequence, 
				Convert.ToScriptExecutionContext<RestartSubscriberScriptData>(message.Metadata));
		}
	}
	
}
